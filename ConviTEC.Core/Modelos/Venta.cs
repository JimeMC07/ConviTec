using ConviTEC.Core.Estructuras;

namespace ConviTEC.Core.Modelos
{
    /// <summary>
    /// Registro de una venta realizada en una sede
    /// (se crea cuando un pedido se entrega).
    /// </summary>
    public class Venta
    {
        // ---- Datos de la venta ----
        public string Id { get; private set; }
        public string IdPedido { get; private set; }
        public string CodigoSede { get; private set; }
        public DateTime Fecha { get; private set; }
        public Lista<LineaPedido> Lineas { get; private set; }   // productos, cantidades y precios

        // Monto total = suma de los subtotales
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
        public Venta(string id, string idPedido, string codigoSede, DateTime fecha)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("El id de la venta no puede estar vacío.");

            if (string.IsNullOrWhiteSpace(codigoSede))
                throw new ArgumentException("La sede de la venta no puede estar vacía.");

            Id = id.Trim();
            IdPedido = idPedido ?? "";
            CodigoSede = codigoSede.Trim();
            Fecha = fecha;
            Lineas = new Lista<LineaPedido>();
        }

        // ---- Agregar un producto vendido ----
        public void AgregarLinea(LineaPedido linea)
        {
            if (linea == null)
                throw new ArgumentException("La línea no puede ser nula.");
            Lineas.Agregar(linea);
        }

        // ---- Crear una venta a partir de un pedido entregado ----
        public static Venta DesdePedido(Pedido pedido)
        {
            if (pedido == null)
                throw new ArgumentException("El pedido no puede ser nulo.");

            if (pedido.Estado != EstadoPedido.ENTREGADO)
                throw new InvalidOperationException("Solo se puede registrar la venta de un pedido entregado.");

            DateTime fecha = pedido.FechaEntrega ?? DateTime.Now;
            Venta venta = new Venta("V-" + pedido.Id, pedido.Id, pedido.CodigoSedeOrigen, fecha);

            foreach (LineaPedido linea in pedido.Lineas)
                venta.AgregarLinea(linea);

            return venta;
        }

        // ---- Texto para mostrar la venta ----
        public override string ToString()
        {
            return $"Venta {Id} | Sede {CodigoSede} | {Fecha:dd/MM/yyyy HH:mm} | {Lineas.Cantidad} productos | ₡{MontoTotal}";
        }
    }
}