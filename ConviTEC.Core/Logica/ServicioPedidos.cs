using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Mensajeria;
using ConviTEC.Core.Modelos;
using ConviTEC.Core.Persistencia;
using ConviTEC.Core.Rutas;

namespace ConviTEC.Core.Logica
{
    /// <summary>Un producto que el dependiente pide (lo que llega desde la interfaz).</summary>
    public class LineaSolicitada
    {
        public string CodigoProducto { get; set; } = "";
        public int Cantidad { get; set; }

        public LineaSolicitada() { }

        public LineaSolicitada(string codigoProducto, int cantidad)
        {
            CodigoProducto = codigoProducto;
            Cantidad = cantidad;
        }
    }

    /// <summary>Resultado de intentar registrar un pedido.</summary>
    public class ResultadoPedido
    {
        public bool Aceptado { get; private set; }
        public string Mensaje { get; private set; }
        public Pedido? Pedido { get; private set; }   // null si ni siquiera se creó (datos inválidos)

        public ResultadoPedido(bool aceptado, string mensaje, Pedido? pedido)
        {
            Aceptado = aceptado;
            Mensaje = mensaje;
            Pedido = pedido;
        }
    }

    /// <summary>
    /// Registra los pedidos de una sede: valida ruta, reserva en las sedes necesarias,
    /// cancela y libera si algo falla, y guarda el historial, los pendientes y las ventas.
    /// </summary>
    public class ServicioPedidos
    {
        private class ReservaHecha
        {
            public string Sede { get; set; } = "";
            public string CodigoProducto { get; set; } = "";
        }

        private readonly ServicioSede sede;
        private readonly Grafo grafo;
        private readonly SemaphoreSlim turno = new SemaphoreSlim(1, 1);   // un pedido a la vez
        private readonly object candado = new object();

        private RepositorioPedidos? repo;
        private Lista<Pedido> historial = new Lista<Pedido>();
        private Cola<Pedido> pendientes = new Cola<Pedido>();
        private Lista<Venta> ventas = new Lista<Venta>();

        public Grafo Grafo
        {
            get { return grafo; }
        }

        public ServicioPedidos(ServicioSede sede, Grafo grafo)
        {
            this.sede = sede ?? throw new ArgumentException("La sede no puede ser nula.");
            this.grafo = grafo ?? throw new ArgumentException("El grafo no puede ser nulo.");
        }

        // =========================================================
        //  INICIAR: cargar historial, pendientes y ventas
        // =========================================================
        public async Task IniciarAsync()
        {
            repo = new RepositorioPedidos(sede.CarpetaSede);
            historial = repo.CargarPedidos();
            ventas = repo.CargarVentas();
            pendientes = new Cola<Pedido>();

            foreach (Pedido p in historial)
            {
                if (p.Estado == EstadoPedido.PENDIENTE)
                    pendientes.Encolar(p);   // la cola se reconstruye desde el historial
                else if (p.Estado == EstadoPedido.EN_VALIDACION)
                {
                    // Se cerró el programa a mitad de la validación: se cancela y se liberan sus reservas
                    foreach (LineaPedido linea in p.Lineas)
                        if (linea.CodigoSedeSuministra != "" && linea.CodigoSedeSuministra != sede.Sede.Codigo)
                            await sede.LiberarReservaRemotaAsync(linea.CodigoSedeSuministra, p.Id, linea.CodigoProducto);

                    p.Cancelar("El sistema se cerró mientras se validaba el pedido.");
                }
            }
            Guardar();
        }

        // =========================================================
        //  REGISTRAR UN PEDIDO
        // =========================================================
        public async Task<ResultadoPedido> RegistrarPedidoAsync(LineaSolicitada[] lineas, string destino)
        {
            VerificarIniciado();
            await turno.WaitAsync();   // si llegan dos pedidos a la vez, se atienden en orden
            try
            {
                return await RegistrarInternoAsync(lineas, destino);
            }
            finally
            {
                turno.Release();
            }
        }

        private async Task<ResultadoPedido> RegistrarInternoAsync(LineaSolicitada[] lineas, string destino)
        {
            // ---- 1. Validar los datos (sin crear el pedido) ----
            if (lineas == null || lineas.Length == 0)
                return new ResultadoPedido(false, "El pedido debe tener al menos un producto.", null);

            if (string.IsNullOrWhiteSpace(destino))
                return new ResultadoPedido(false, "Debe indicar el punto de entrega.", null);
            destino = destino.Trim();

            for (int i = 0; i < lineas.Length; i++)
            {
                if (lineas[i] == null || string.IsNullOrWhiteSpace(lineas[i].CodigoProducto))
                    return new ResultadoPedido(false, "Hay un producto sin código.", null);
                if (lineas[i].Cantidad <= 0)
                    return new ResultadoPedido(false, $"La cantidad de {lineas[i].CodigoProducto} debe ser mayor que cero.", null);

                for (int j = 0; j < i; j++)
                    if (lineas[j].CodigoProducto.Trim() == lineas[i].CodigoProducto.Trim())
                        return new ResultadoPedido(false, $"El producto {lineas[i].CodigoProducto} está repetido en el pedido.", null);
            }

            if (!grafo.ExisteVertice(destino))
                return new ResultadoPedido(false, $"El punto de entrega '{destino}' no existe en el mapa de rutas.", null);

            // ---- 2. Crear el pedido y guardarlo en el historial ----
            Pedido pedido = new Pedido(GenerarId(), sede.Sede.Codigo, destino, DateTime.Now);
            lock (candado) historial.Agregar(pedido);

            // ---- 3. Validar la ruta ----
            if (!ExisteRuta(destino))
            {
                pedido.Rechazar($"No existe ruta de entrega desde {sede.Sede.Nombre} hasta {destino}.");
                Guardar();
                return new ResultadoPedido(false, pedido.Motivo, pedido);
            }

            // ---- 4. Reservar cada producto en alguna sede ----
            Lista<ReservaHecha> hechas = new Lista<ReservaHecha>();
            string motivo = "";

            for (int i = 0; i < lineas.Length; i++)
            {
                string codigo = lineas[i].CodigoProducto.Trim();
                int cantidad = lineas[i].Cantidad;

                EntradaCatalogo? info = sede.ConsultarGlobal(codigo);
                if (info == null)
                {
                    motivo = $"El producto {codigo} no existe en el catálogo.";
                    break;
                }

                string sedeQueSuministra = await ReservarEnAlgunaSedeAsync(pedido.Id, codigo, cantidad);
                if (sedeQueSuministra == "")
                {
                    motivo = $"No hay suficiente {info.Producto.Nombre} en ninguna sede (se pidieron {cantidad}).";
                    break;
                }

                hechas.Agregar(new ReservaHecha { Sede = sedeQueSuministra, CodigoProducto = codigo });

                LineaPedido linea = new LineaPedido(codigo, info.Producto.Nombre, cantidad, info.Producto.Precio);
                linea.AsignarSede(sedeQueSuministra);
                pedido.AgregarLinea(linea);
                pedido.AgregarSedeParticipante(sedeQueSuministra);
            }

            // ---- 5a. Algo falló: liberar TODAS las reservas y cancelar ----
            if (motivo != "")
            {
                foreach (ReservaHecha r in hechas)
                    await LiberarAsync(pedido.Id, r);

                pedido.Cancelar(motivo + " Se liberaron las reservas.");
                Guardar();
                return new ResultadoPedido(false, pedido.Motivo, pedido);
            }

            // ---- 5b. Todo bien: confirmar (rebajar) en todas las sedes ----
            foreach (ReservaHecha r in hechas)
                await ConfirmarAsync(pedido.Id, r);

            pedido.Confirmar();
            lock (candado) pendientes.Encolar(pedido);
            Guardar();
            return new ResultadoPedido(true, $"Pedido {pedido.Id} aceptado. Total: ₡{pedido.MontoTotal}.", pedido);
        }

        // ---- Primero intenta en mi sede; si no, en las que tengan suficiente ----
        // Devuelve el código de la sede que reservó, o "" si ninguna pudo.
        private async Task<string> ReservarEnAlgunaSedeAsync(string idPedido, string codigo, int cantidad)
        {
            string propia = sede.Sede.Codigo;

            if (sede.ReservarParaPedido(idPedido, codigo, cantidad) == "")
            {
                await sede.DifundirCambioAsync(codigo);
                return propia;
            }

            Lista<ExistenciaSede> candidatas = sede.SedesConDisponibilidad(codigo);
            foreach (ExistenciaSede e in candidatas)
            {
                if (e.CodigoSede == propia || e.Cantidad < cantidad) continue;

                Mensaje? respuesta = await sede.SolicitarReservaRemotaAsync(e.CodigoSede, idPedido, codigo, cantidad);
                if (respuesta != null && respuesta.Aceptado)
                    return e.CodigoSede;
            }
            return "";
        }

        private async Task LiberarAsync(string idPedido, ReservaHecha r)
        {
            if (r.Sede == sede.Sede.Codigo)
            {
                if (sede.LiberarReservaDePedido(idPedido, r.CodigoProducto))
                    await sede.DifundirCambioAsync(r.CodigoProducto);
            }
            else
                await sede.LiberarReservaRemotaAsync(r.Sede, idPedido, r.CodigoProducto);
        }

        private async Task ConfirmarAsync(string idPedido, ReservaHecha r)
        {
            if (r.Sede == sede.Sede.Codigo)
            {
                if (sede.ConfirmarReservaDePedido(idPedido, r.CodigoProducto))
                    await sede.DifundirCambioAsync(r.CodigoProducto);
            }
            else
                await sede.ConfirmarReservaRemotaAsync(r.Sede, idPedido, r.CodigoProducto);
        }

        // =========================================================
        //  RUTAS
        // =========================================================
        public bool ExisteRuta(string destino)
        {
            if (!grafo.ExisteVertice(sede.Sede.Ubicacion) || !grafo.ExisteVertice(destino))
                return false;
            return Dijkstra.Calcular(grafo, sede.Sede.Ubicacion).EsAlcanzable(destino);
        }

        // =========================================================
        //  ENTREGA (lo usará la Persona B)
        // =========================================================
        public Venta MarcarEntregado(string idPedido, int tiempoTraslado)
        {
            VerificarIniciado();
            lock (candado)
            {
                Pedido? pedido = pendientes.Buscar(p => p.Id == idPedido);
                if (pedido == null)
                    throw new ArgumentException($"El pedido {idPedido} no está pendiente de entrega.");

                pedido.MarcarEntregado(tiempoTraslado, DateTime.Now);
                pendientes.Eliminar(p => p.Id == idPedido);

                Venta venta = Venta.DesdePedido(pedido);
                ventas.Agregar(venta);
                Guardar();
                return venta;
            }
        }

        // =========================================================
        //  CONSULTAS (devuelven copias para que nadie dañe las originales)
        // =========================================================
        public Lista<Pedido> ObtenerPendientes()
        {
            Lista<Pedido> copia = new Lista<Pedido>();
            lock (candado) foreach (Pedido p in pendientes) copia.Agregar(p);
            return copia;
        }

        public Lista<Pedido> ObtenerHistorial()
        {
            Lista<Pedido> copia = new Lista<Pedido>();
            lock (candado) foreach (Pedido p in historial) copia.Agregar(p);
            return copia;
        }

        public Lista<Venta> ObtenerVentas()
        {
            Lista<Venta> copia = new Lista<Venta>();
            lock (candado) foreach (Venta v in ventas) copia.Agregar(v);
            return copia;
        }

        public Pedido? ConsultarPedido(string idPedido)
        {
            lock (candado) return historial.Buscar(p => p.Id == idPedido);
        }

        // =========================================================
        //  AYUDANTES
        // =========================================================

        // Id único: código de la sede + número, ej. "S3-0007"
        private string GenerarId()
        {
            lock (candado)
            {
                int numero = historial.Cantidad + 1;
                string id = $"{sede.Sede.Codigo}-{numero:D4}";
                while (historial.Existe(p => p.Id == id))
                {
                    numero++;
                    id = $"{sede.Sede.Codigo}-{numero:D4}";
                }
                return id;
            }
        }

        private void Guardar()
        {
            lock (candado)
            {
                repo!.GuardarPedidos(historial);
                repo.GuardarVentas(ventas);
            }
        }

        private void VerificarIniciado()
        {
            if (repo == null)
                throw new InvalidOperationException("El servicio de pedidos no se ha iniciado. Llame primero a IniciarAsync().");
        }
    }
}