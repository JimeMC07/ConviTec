using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Mensajeria;
using ConviTEC.Core.Modelos;
using ConviTEC.Core.Persistencia;

namespace ConviTEC.Core.Logica
{
    /// <summary>Configuración para encender una sede.</summary>
    public class OpcionesSede
    {
        public string CodigoSede { get; set; } = "";
        public string CarpetaDatos { get; set; } = "datos";
        public string HostRabbit { get; set; } = "localhost";
        public int PuertoRabbit { get; set; } = 5672;
        public string UsuarioRabbit { get; set; } = "guest";
        public string ClaveRabbit { get; set; } = "guest";
        public int TiempoEsperaMs { get; set; } = 5000;   // cuánto esperar una respuesta
    }

    /// <summary>Una reserva hecha para un pedido (qué producto y cuánto).</summary>
    public class ReservaActiva
    {
        public string IdPedido { get; private set; }
        public string CodigoProducto { get; private set; }
        public int Cantidad { get; private set; }

        public ReservaActiva(string idPedido, string codigoProducto, int cantidad)
        {
            IdPedido = idPedido;
            CodigoProducto = codigoProducto;
            Cantidad = cantidad;
        }
    }

    /// <summary>
    /// El "cerebro" de una sede: maneja su inventario, lo guarda en archivos,
    /// y se comunica con las demás sedes por mensajes.
    /// </summary>
    public class ServicioSede : IAsyncDisposable
    {
        private readonly OpcionesSede opciones;
        private readonly object candado = new object();   // evita que dos cosas cambien el inventario a la vez
        private readonly Lista<ReservaActiva> reservas = new Lista<ReservaActiva>();
        private readonly GestorRespuestas respuestas = new GestorRespuestas();
        private readonly InventarioGlobal global = new InventarioGlobal();

        private Lista<Sede> sedes = new Lista<Sede>();
        private Sede? sede;
        private InventarioLocal? inventario;
        private RepositorioInventario? repoInventario;
        private ClienteMensajeria? cliente;

        public string CarpetaSede { get; private set; } = "";

        public Sede Sede
        {
            get { return sede ?? throw new InvalidOperationException("La sede todavía no se ha iniciado."); }
        }

        public Lista<Sede> Sedes
        {
            get { return sedes; }
        }

        public ServicioSede(OpcionesSede opciones)
        {
            if (opciones == null || string.IsNullOrWhiteSpace(opciones.CodigoSede))
                throw new ArgumentException("Debe indicar el código de la sede.");
            this.opciones = opciones;
        }

        // =========================================================
        //  ENCENDER LA SEDE
        // =========================================================
        public async Task IniciarAsync()
        {
            // 1. Saber quién soy
            sedes = CargadorConfiguracion.CargarSedes(Path.Combine(opciones.CarpetaDatos, "sedes.json"));
            sede = sedes.Buscar(s => s.Codigo == opciones.CodigoSede);
            if (sede == null)
                throw new InvalidOperationException($"La sede {opciones.CodigoSede} no está en sedes.json.");

            // 2. Cargar mi inventario (o el inicial si es la primera vez)
            CarpetaSede = Path.Combine(opciones.CarpetaDatos, sede.Codigo);
            repoInventario = new RepositorioInventario(CarpetaSede);

            if (File.Exists(Path.Combine(CarpetaSede, "inventario.json")))
                inventario = repoInventario.Cargar(sede.Codigo);
            else
            {
                inventario = CargadorConfiguracion.CargarInventarioInicial(
                    Path.Combine(opciones.CarpetaDatos, "inventario_inicial.json"), sede.Codigo);
                repoInventario.Guardar(inventario);
            }

            // 3. Mi parte del inventario global
            foreach (ItemInventario item in inventario.ListarTodo())
                global.Actualizar(sede.Codigo, item.Producto, item.Disponible);

            // 4. Conectarme a RabbitMQ y empezar a escuchar
            cliente = new ClienteMensajeria(sede.Codigo, opciones.HostRabbit, opciones.PuertoRabbit,
                                            opciones.UsuarioRabbit, opciones.ClaveRabbit);
            await cliente.ConectarAsync();
            await cliente.EscucharAsync(ProcesarMensajeAsync);

            // 5. Avisar a todas qué tengo, y preguntarles qué tienen ellas
            foreach (ItemInventario item in inventario.ListarTodo())
                await cliente.DifundirAsync(Mensaje.ActualizacionInventario(sede.Codigo, item.Producto, item.Disponible));

            await cliente.DifundirAsync(Mensaje.SolicitarInventario(sede.Codigo));
        }

        // =========================================================
        //  CARTAS QUE LLEGAN
        // =========================================================
        private async Task ProcesarMensajeAsync(Mensaje m)
        {
            if (!EsSedeConocida(m.SedeOrigen)) return;   // ignora sedes que no están en sedes.json

            switch (m.Tipo)
            {
                case TipoMensaje.ACTUALIZACION_INVENTARIO:
                    Producto producto = new Producto(m.CodigoProducto, m.NombreProducto, m.Descripcion, m.Categoria, m.Precio);
                    lock (candado)
                        global.Actualizar(m.SedeOrigen, producto, m.Cantidad);
                    break;

                case TipoMensaje.SOLICITAR_INVENTARIO:
                    await EnviarMiInventarioAAsync(m.SedeOrigen);
                    break;

                case TipoMensaje.SOLICITUD_RESERVA:
                    string error = ReservarParaPedido(m.IdPedido, m.CodigoProducto, m.Cantidad);
                    bool aceptado = error == "";
                    await cliente!.EnviarAsync(Mensaje.RespuestaReserva(m, Sede.Codigo, aceptado,
                                               aceptado ? "Reserva realizada" : error));
                    if (aceptado) await DifundirCambioAsync(m.CodigoProducto);
                    break;

                case TipoMensaje.CONFIRMAR_RESERVA:
                    if (ConfirmarReservaDePedido(m.IdPedido, m.CodigoProducto))
                        await DifundirCambioAsync(m.CodigoProducto);
                    break;

                case TipoMensaje.LIBERAR_RESERVA:
                    if (LiberarReservaDePedido(m.IdPedido, m.CodigoProducto))
                        await DifundirCambioAsync(m.CodigoProducto);
                    break;

                case TipoMensaje.RESPUESTA_RESERVA:
                    respuestas.Resolver(m);
                    break;
            }
        }

        // =========================================================
        //  RESERVAS EN MI INVENTARIO (las uso yo o me las piden otras sedes)
        // =========================================================

        // Devuelve "" si se pudo reservar, o el motivo si no.
        public string ReservarParaPedido(string idPedido, string codigoProducto, int cantidad)
        {
            lock (candado)
            {
                if (reservas.Existe(r => r.IdPedido == idPedido && r.CodigoProducto == codigoProducto))
                    return "";   // ya estaba reservada (mensaje repetido): no se reserva dos veces

                if (cantidad <= 0)
                    return "La cantidad debe ser mayor que cero.";

                ItemInventario? item = inventario!.Consultar(codigoProducto);
                if (item == null)
                    return $"La sede {Sede.Nombre} no tiene el producto {codigoProducto}.";

                if (item.Disponible < cantidad)
                    return $"La sede {Sede.Nombre} solo tiene {item.Disponible} de {item.Producto.Nombre} (se pidieron {cantidad}).";

                item.Reservar(cantidad);
                reservas.Agregar(new ReservaActiva(idPedido, codigoProducto, cantidad));
                GuardarYActualizarGlobal(item);
                return "";
            }
        }

        public bool ConfirmarReservaDePedido(string idPedido, string codigoProducto)
        {
            lock (candado)
            {
                ReservaActiva? r = reservas.Buscar(x => x.IdPedido == idPedido && x.CodigoProducto == codigoProducto);
                if (r == null) return false;   // no existe (o ya se confirmó)

                inventario!.ConfirmarReserva(codigoProducto, r.Cantidad);
                reservas.Eliminar(x => x == r);
                GuardarYActualizarGlobal(inventario.Consultar(codigoProducto)!);
                return true;
            }
        }

        public bool LiberarReservaDePedido(string idPedido, string codigoProducto)
        {
            lock (candado)
            {
                ReservaActiva? r = reservas.Buscar(x => x.IdPedido == idPedido && x.CodigoProducto == codigoProducto);
                if (r == null) return false;   // no existe (o ya se liberó)

                inventario!.LiberarReserva(codigoProducto, r.Cantidad);
                reservas.Eliminar(x => x == r);
                GuardarYActualizarGlobal(inventario.Consultar(codigoProducto)!);
                return true;
            }
        }

        // =========================================================
        //  PEDIRLE COSAS A OTRA SEDE
        // =========================================================

        // Devuelve la respuesta, o null si la otra sede no contestó a tiempo.
        public async Task<Mensaje?> SolicitarReservaRemotaAsync(string sedeDestino, string idPedido, string codigoProducto, int cantidad)
        {
            VerificarIniciada();
            Mensaje solicitud = Mensaje.SolicitudReserva(Sede.Codigo, sedeDestino, idPedido, codigoProducto, cantidad);
            Task<Mensaje> espera = respuestas.Registrar(solicitud.Id);

            await cliente!.EnviarAsync(solicitud);

            Task terminada = await Task.WhenAny(espera, Task.Delay(opciones.TiempoEsperaMs));
            if (terminada == espera)
                return await espera;

            // No contestó a tiempo: dejo de esperar y, por si reservó tarde, le pido que libere
            respuestas.Cancelar(solicitud.Id);
            await cliente.EnviarAsync(Mensaje.LiberarReserva(Sede.Codigo, sedeDestino, idPedido, codigoProducto, cantidad));
            return null;
        }

        public async Task ConfirmarReservaRemotaAsync(string sedeDestino, string idPedido, string codigoProducto)
        {
            VerificarIniciada();
            await cliente!.EnviarAsync(Mensaje.ConfirmarReserva(Sede.Codigo, sedeDestino, idPedido, codigoProducto, 0));
        }

        public async Task LiberarReservaRemotaAsync(string sedeDestino, string idPedido, string codigoProducto)
        {
            VerificarIniciada();
            await cliente!.EnviarAsync(Mensaje.LiberarReserva(Sede.Codigo, sedeDestino, idPedido, codigoProducto, 0));
        }

        // =========================================================
        //  OPERACIONES DE MI SEDE
        // =========================================================

        // Incorporar nuevas unidades al inventario local
        public async Task AgregarUnidadesAsync(string codigoProducto, int cantidad)
        {
            VerificarIniciada();
            lock (candado)
            {
                inventario!.AgregarUnidades(codigoProducto, cantidad);
                GuardarYActualizarGlobal(inventario.Consultar(codigoProducto)!);
            }
            await DifundirCambioAsync(codigoProducto);
        }

        // Registrar un producto que esta sede no tenía
        public async Task RegistrarProductoAsync(Producto producto, int cantidadInicial)
        {
            VerificarIniciada();
            lock (candado)
            {
                inventario!.RegistrarProducto(producto, cantidadInicial);
                GuardarYActualizarGlobal(inventario.Consultar(producto.Codigo)!);
            }
            await DifundirCambioAsync(producto.Codigo);
        }

        // =========================================================
        //  CONSULTAS
        // =========================================================

        public ItemInventario? ConsultarLocal(string codigoProducto)
        {
            lock (candado) return inventario?.Consultar(codigoProducto);
        }

        public Lista<ItemInventario> ListarInventarioLocal()
        {
            lock (candado) return inventario == null ? new Lista<ItemInventario>() : inventario.ListarTodo();
        }

        public Lista<EntradaCatalogo> CatalogoGlobal()
        {
            lock (candado) return global.ListarCatalogo();
        }

        public EntradaCatalogo? ConsultarGlobal(string codigoProducto)
        {
            lock (candado) return global.Consultar(codigoProducto);
        }

        public int CantidadEnSede(string codigoProducto, string codigoSede)
        {
            lock (candado) return global.CantidadEn(codigoProducto, codigoSede);
        }

        public Lista<ExistenciaSede> SedesConDisponibilidad(string codigoProducto)
        {
            lock (candado) return global.SedesConDisponibilidad(codigoProducto);
        }

        // =========================================================
        //  AYUDANTES
        // =========================================================

        private bool EsSedeConocida(string codigoSede)
        {
            return sedes.Existe(s => s.Codigo == codigoSede);
        }

        // (se llama dentro de lock) guarda el archivo y actualiza mi parte del global
        private void GuardarYActualizarGlobal(ItemInventario item)
        {
            repoInventario!.Guardar(inventario!);
            global.Actualizar(Sede.Codigo, item.Producto, item.Disponible);
        }

        private async Task DifundirCambioAsync(string codigoProducto)
        {
            ItemInventario? item;
            lock (candado) item = inventario!.Consultar(codigoProducto);
            if (item != null)
                await cliente!.DifundirAsync(Mensaje.ActualizacionInventario(Sede.Codigo, item.Producto, item.Disponible));
        }

        private async Task EnviarMiInventarioAAsync(string sedeDestino)
        {
            ItemInventario[] items;
            lock (candado) items = inventario!.ListarTodo().ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                Mensaje m = Mensaje.ActualizacionInventario(Sede.Codigo, items[i].Producto, items[i].Disponible);
                m.SedeDestino = sedeDestino;   // solo a la que preguntó
                await cliente!.EnviarAsync(m);
            }
        }

        private void VerificarIniciada()
        {
            if (sede == null || inventario == null || cliente == null)
                throw new InvalidOperationException("La sede no se ha iniciado. Llame primero a IniciarAsync().");
        }

        public async ValueTask DisposeAsync()
        {
            if (cliente != null) await cliente.DisposeAsync();
        }
    }
}