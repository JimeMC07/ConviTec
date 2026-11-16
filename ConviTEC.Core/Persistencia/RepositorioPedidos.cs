using System.Text.Encodings.Web;
using System.Text.Json;
using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Modelos;

namespace ConviTEC.Core.Persistencia
{
    /// <summary>
    /// Guarda y carga los pedidos (historial) y las ventas de una sede.
    /// </summary>
    public class RepositorioPedidos
    {
        private readonly string rutaPedidos;
        private readonly string rutaVentas;

        private static readonly JsonSerializerOptions opciones = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public RepositorioPedidos(string carpetaDatos)
        {
            if (string.IsNullOrWhiteSpace(carpetaDatos))
                throw new ArgumentException("La carpeta de datos no puede estar vacía.");

            Directory.CreateDirectory(carpetaDatos);
            rutaPedidos = Path.Combine(carpetaDatos, "pedidos.json");
            rutaVentas = Path.Combine(carpetaDatos, "ventas.json");
        }

        // =====================  PEDIDOS  =====================

        public void GuardarPedidos(Lista<Pedido> historial)
        {
            Pedido[] pedidos = historial.ToArray();
            PedidoDTO[] dtos = new PedidoDTO[pedidos.Length];

            for (int i = 0; i < pedidos.Length; i++)
            {
                Pedido p = pedidos[i];
                dtos[i] = new PedidoDTO
                {
                    Id = p.Id,
                    FechaCreacion = p.FechaCreacion,
                    Estado = p.Estado.ToString(),
                    CodigoSedeOrigen = p.CodigoSedeOrigen,
                    Destino = p.Destino,
                    Motivo = p.Motivo,
                    TiempoTraslado = p.TiempoTraslado,
                    FechaEntrega = p.FechaEntrega,
                    Lineas = LineaDTO.DesdeLineas(p.Lineas.ToArray()),
                    SedesParticipantes = p.SedesParticipantes.ToArray()
                };
            }
            EscribirArchivo(rutaPedidos, JsonSerializer.Serialize(dtos, opciones));
        }

        public Lista<Pedido> CargarPedidos()
        {
            Lista<Pedido> historial = new Lista<Pedido>();
            PedidoDTO[]? dtos = LeerArchivo<PedidoDTO>(rutaPedidos);
            if (dtos == null) return historial;

            for (int i = 0; i < dtos.Length; i++)
            {
                PedidoDTO d = dtos[i];

                // ignora pedidos repetidos en el archivo
                if (historial.Existe(x => x.Id == d.Id)) continue;

                Pedido pedido = new Pedido(d.Id, d.CodigoSedeOrigen, d.Destino, d.FechaCreacion);

                for (int j = 0; j < d.Lineas.Length; j++)
                    pedido.AgregarLinea(d.Lineas[j].ALinea());

                for (int j = 0; j < d.SedesParticipantes.Length; j++)
                    pedido.AgregarSedeParticipante(d.SedesParticipantes[j]);

                // texto → enum (si el texto es inválido, queda CANCELADO para no dar problemas)
                EstadoPedido estado;
                if (!Enum.TryParse(d.Estado, out estado))
                    estado = EstadoPedido.CANCELADO;

                pedido.Restaurar(estado, d.Motivo, d.TiempoTraslado, d.FechaEntrega);
                historial.Agregar(pedido);
            }
            return historial;
        }

        // =====================  VENTAS  =====================

        public void GuardarVentas(Lista<Venta> ventas)
        {
            Venta[] arreglo = ventas.ToArray();
            VentaDTO[] dtos = new VentaDTO[arreglo.Length];

            for (int i = 0; i < arreglo.Length; i++)
            {
                Venta v = arreglo[i];
                dtos[i] = new VentaDTO
                {
                    Id = v.Id,
                    IdPedido = v.IdPedido,
                    CodigoSede = v.CodigoSede,
                    Fecha = v.Fecha,
                    Lineas = LineaDTO.DesdeLineas(v.Lineas.ToArray())
                };
            }
            EscribirArchivo(rutaVentas, JsonSerializer.Serialize(dtos, opciones));
        }

        public Lista<Venta> CargarVentas()
        {
            Lista<Venta> ventas = new Lista<Venta>();
            VentaDTO[]? dtos = LeerArchivo<VentaDTO>(rutaVentas);
            if (dtos == null) return ventas;

            for (int i = 0; i < dtos.Length; i++)
            {
                VentaDTO d = dtos[i];
                if (ventas.Existe(x => x.Id == d.Id)) continue;

                Venta venta = new Venta(d.Id, d.IdPedido, d.CodigoSede, d.Fecha);
                for (int j = 0; j < d.Lineas.Length; j++)
                    venta.AgregarLinea(d.Lineas[j].ALinea());

                ventas.Agregar(venta);
            }
            return ventas;
        }

        // =====================  AYUDANTES  =====================

        // Escribe con archivo temporal para no dañar el original si algo falla
        private void EscribirArchivo(string ruta, string contenido)
        {
            string rutaTemporal = ruta + ".tmp";
            File.WriteAllText(rutaTemporal, contenido);
            File.Move(rutaTemporal, ruta, true);
        }

        // Lee un arreglo del archivo; null si no existe
        private T[]? LeerArchivo<T>(string ruta)
        {
            if (!File.Exists(ruta)) return null;
            try
            {
                return JsonSerializer.Deserialize<T[]>(File.ReadAllText(ruta), opciones);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"El archivo {ruta} está dañado o mal escrito: {ex.Message}");
            }
        }
    }
}