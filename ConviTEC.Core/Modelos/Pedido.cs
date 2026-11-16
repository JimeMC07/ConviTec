using ConviTEC.Core.Estructuras;

namespace ConviTEC.Core.Modelos
{
    /// <summary>
    /// Estados por los que pasa un pedido.
    /// </summary>
    public enum EstadoPedido
    {
        EN_VALIDACION,  // recién creado, revisando ruta y reservas
        PENDIENTE,      // aceptado, esperando entrega
        RECHAZADO,      // no se pudo aceptar (sin ruta o sin inventario)
        CANCELADO,      // se canceló y se liberaron las reservas
        ENTREGADO       // ya se entregó
    }

    /// <summary>
    /// Un pedido registrado en una sede.
    /// </summary>
    public class Pedido
    {
        // ---- Datos del pedido ----
        public string Id { get; private set; }
        public DateTime FechaCreacion { get; private set; }
        public EstadoPedido Estado { get; private set; }
        public string CodigoSedeOrigen { get; private set; }   // sede donde se registró
        public string Destino { get; private set; }            // punto de entrega (nombre en el grafo)
        public string Motivo { get; private set; }             // explicación si se rechaza o cancela

        public Lista<LineaPedido> Lineas { get; private set; }
        public Lista<string> SedesParticipantes { get; private set; }

        // Se llenan al entregar
        public int TiempoTraslado { get; private set; }        // minutos
        public DateTime? FechaEntrega { get; private set; }

        // El monto total se calcula sumando los subtotales de las líneas
        public decimal MontoTotal
        {
            get
            {
                decimal total = 0;
                foreach (LineaPedido linea in Lineas)
                    total += linea.Subtotal;
                return total;
            }
        }

        // ---- Constructor ----
        public Pedido(string id, string codigoSedeOrigen, string destino, DateTime fechaCreacion)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("El id del pedido no puede estar vacío.");

            if (string.IsNullOrWhiteSpace(codigoSedeOrigen))
                throw new ArgumentException("La sede del pedido no puede estar vacía.");

            if (string.IsNullOrWhiteSpace(destino))
                throw new ArgumentException("El destino del pedido no puede estar vacío.");

            Id = id.Trim();
            CodigoSedeOrigen = codigoSedeOrigen.Trim();
            Destino = destino.Trim();
            FechaCreacion = fechaCreacion;
            Estado = EstadoPedido.EN_VALIDACION;
            Motivo = "";
            Lineas = new Lista<LineaPedido>();
            SedesParticipantes = new Lista<string>();
            TiempoTraslado = 0;
            FechaEntrega = null;
        }

        // ---- Agregar un producto al pedido ----
        // Si el producto ya estaba, no se duplica: se rechaza.
        public void AgregarLinea(LineaPedido linea)
        {
            if (linea == null)
                throw new ArgumentException("La línea no puede ser nula.");

            if (Estado != EstadoPedido.EN_VALIDACION)
                throw new InvalidOperationException("Solo se pueden agregar productos a un pedido en validación.");

            if (Lineas.Existe(l => l.CodigoProducto == linea.CodigoProducto))
                throw new ArgumentException($"El producto {linea.CodigoProducto} ya está en el pedido.");

            Lineas.Agregar(linea);
        }

        // ---- Registrar una sede que aporta productos (sin repetir) ----
        public void AgregarSedeParticipante(string codigoSede)
        {
            if (string.IsNullOrWhiteSpace(codigoSede))
                throw new ArgumentException("El código de la sede no puede estar vacío.");

            if (!SedesParticipantes.Existe(s => s == codigoSede))
                SedesParticipantes.Agregar(codigoSede);
        }

        // ---- Cambios de estado ----
        public void Confirmar()
        {
            if (Estado != EstadoPedido.EN_VALIDACION)
                throw new InvalidOperationException("Solo se puede confirmar un pedido en validación.");

            if (Lineas.EstaVacia())
                throw new InvalidOperationException("No se puede confirmar un pedido sin productos.");

            Estado = EstadoPedido.PENDIENTE;
        }

        public void Rechazar(string motivo)
        {
            if (Estado != EstadoPedido.EN_VALIDACION)
                throw new InvalidOperationException("Solo se puede rechazar un pedido en validación.");

            Estado = EstadoPedido.RECHAZADO;
            Motivo = motivo ?? "";
        }

        public void Cancelar(string motivo)
        {
            if (Estado != EstadoPedido.EN_VALIDACION && Estado != EstadoPedido.PENDIENTE)
                throw new InvalidOperationException("Solo se puede cancelar un pedido en validación o pendiente.");

            Estado = EstadoPedido.CANCELADO;
            Motivo = motivo ?? "";
        }

        public void MarcarEntregado(int tiempoTraslado, DateTime fechaEntrega)
        {
            if (Estado != EstadoPedido.PENDIENTE)
                throw new InvalidOperationException("Solo se puede entregar un pedido pendiente.");

            if (tiempoTraslado < 0)
                throw new ArgumentException("El tiempo de traslado no puede ser negativo.");

            Estado = EstadoPedido.ENTREGADO;
            TiempoTraslado = tiempoTraslado;
            FechaEntrega = fechaEntrega;
        }

        // ---- Restaurar el estado al cargar desde archivo ----
        // SOLO lo usa la persistencia. No revisa las reglas de cambio de estado
        // porque el pedido ya había pasado por ellas antes de guardarse.
        public void Restaurar(EstadoPedido estado, string motivo, int tiempoTraslado, DateTime? fechaEntrega)
        {
            Estado = estado;
            Motivo = motivo ?? "";
            TiempoTraslado = tiempoTraslado < 0 ? 0 : tiempoTraslado;
            FechaEntrega = fechaEntrega;
        }

        // ---- Texto para mostrar el pedido ----
        public override string ToString()
        {
            return $"Pedido {Id} | {Estado} | Sede {CodigoSedeOrigen} → {Destino} | {Lineas.Cantidad} productos | ₡{MontoTotal}";
        }
    }
}