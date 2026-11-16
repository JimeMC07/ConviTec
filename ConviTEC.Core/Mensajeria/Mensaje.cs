using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConviTEC.Core.Modelos;

namespace ConviTEC.Core.Mensajeria
{
    /// <summary>
    /// Tipos de mensaje que se envían las sedes.
    /// </summary>
    public enum TipoMensaje
    {
        SOLICITUD_RESERVA,
        RESPUESTA_RESERVA,
        CONFIRMAR_RESERVA,
        LIBERAR_RESERVA,
        ACTUALIZACION_INVENTARIO
    }

    /// <summary>
    /// Un mensaje ("carta") que viaja entre sedes por RabbitMQ.
    /// Se convierte a JSON para enviarlo y se lee de JSON al recibirlo.
    /// </summary>
    public class Mensaje
    {
        // ---- Datos generales ----
        public string Id { get; set; } = Guid.NewGuid().ToString("N");   // identificador único del mensaje
        public TipoMensaje Tipo { get; set; }
        public string SedeOrigen { get; set; } = "";
        public string SedeDestino { get; set; } = "";     // vacío = para todas las sedes
        public DateTime Fecha { get; set; } = DateTime.Now;

        // ---- Datos del pedido / reserva ----
        public string IdPedido { get; set; } = "";
        public string CodigoProducto { get; set; } = "";
        public int Cantidad { get; set; }

        // ---- Datos de respuesta ----
        public string IdRespuestaA { get; set; } = "";    // Id del mensaje que se está respondiendo
        public bool Aceptado { get; set; }
        public string Texto { get; set; } = "";           // explicación (ej. "no hay suficiente")

        // ---- Datos del producto (para ACTUALIZACION_INVENTARIO) ----
        public string NombreProducto { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal Precio { get; set; }

        // =========================================================
        //  CONVERTIR A / DESDE JSON
        // =========================================================

        private static readonly JsonSerializerOptions opciones = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() }   // el tipo viaja como texto: "SOLICITUD_RESERVA"
        };

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, opciones);
        }

        public static Mensaje DesdeJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("El mensaje recibido está vacío.");
            try
            {
                Mensaje? m = JsonSerializer.Deserialize<Mensaje>(json, opciones);
                if (m == null || string.IsNullOrWhiteSpace(m.SedeOrigen))
                    throw new InvalidDataException("El mensaje recibido no tiene sede de origen.");
                return m;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("El mensaje recibido está mal formado: " + ex.Message);
            }
        }

        // =========================================================
        //  FÁBRICAS: formas fáciles de crear cada tipo de mensaje
        // =========================================================

        public static Mensaje SolicitudReserva(string sedeOrigen, string sedeDestino, string idPedido, string codigoProducto, int cantidad)
        {
            return new Mensaje
            {
                Tipo = TipoMensaje.SOLICITUD_RESERVA,
                SedeOrigen = sedeOrigen,
                SedeDestino = sedeDestino,
                IdPedido = idPedido,
                CodigoProducto = codigoProducto,
                Cantidad = cantidad
            };
        }

        // Responde a una solicitud: va de vuelta a quien la mandó
        public static Mensaje RespuestaReserva(Mensaje solicitud, string sedeQueResponde, bool aceptado, string texto)
        {
            return new Mensaje
            {
                Tipo = TipoMensaje.RESPUESTA_RESERVA,
                SedeOrigen = sedeQueResponde,
                SedeDestino = solicitud.SedeOrigen,
                IdPedido = solicitud.IdPedido,
                CodigoProducto = solicitud.CodigoProducto,
                Cantidad = solicitud.Cantidad,
                IdRespuestaA = solicitud.Id,
                Aceptado = aceptado,
                Texto = texto ?? ""
            };
        }

        public static Mensaje ConfirmarReserva(string sedeOrigen, string sedeDestino, string idPedido, string codigoProducto, int cantidad)
        {
            Mensaje m = SolicitudReserva(sedeOrigen, sedeDestino, idPedido, codigoProducto, cantidad);
            m.Tipo = TipoMensaje.CONFIRMAR_RESERVA;
            return m;
        }

        public static Mensaje LiberarReserva(string sedeOrigen, string sedeDestino, string idPedido, string codigoProducto, int cantidad)
        {
            Mensaje m = SolicitudReserva(sedeOrigen, sedeDestino, idPedido, codigoProducto, cantidad);
            m.Tipo = TipoMensaje.LIBERAR_RESERVA;
            return m;
        }

        // Aviso para TODAS las sedes: "ahora tengo esta cantidad de este producto"
        public static Mensaje ActualizacionInventario(string sedeOrigen, Producto producto, int cantidadDisponible)
        {
            return new Mensaje
            {
                Tipo = TipoMensaje.ACTUALIZACION_INVENTARIO,
                SedeOrigen = sedeOrigen,
                SedeDestino = "",
                CodigoProducto = producto.Codigo,
                Cantidad = cantidadDisponible,
                NombreProducto = producto.Nombre,
                Descripcion = producto.Descripcion,
                Categoria = producto.Categoria,
                Precio = producto.Precio
            };
        }

        public override string ToString()
        {
            return $"[{Tipo}] {SedeOrigen} → {(SedeDestino == "" ? "TODAS" : SedeDestino)} | pedido {IdPedido} | {CodigoProducto} x{Cantidad}";
        }
    }
}