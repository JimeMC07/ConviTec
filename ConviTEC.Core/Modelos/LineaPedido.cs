namespace ConviTEC.Core.Modelos
{
    /// <summary>
    /// Un renglón de un pedido: qué producto, cuántas unidades,
    /// a qué precio y de qué sede sale.
    /// </summary>
    public class LineaPedido
    {
        // ---- Datos de la línea ----
        public string CodigoProducto { get; private set; }
        public string NombreProducto { get; private set; }
        public int Cantidad { get; private set; }
        public decimal PrecioUnitario { get; private set; }

        // Código de la sede que entrega este producto.
        // Empieza vacío y se asigna cuando el sistema decide de dónde sale.
        public string CodigoSedeSuministra { get; private set; }

        // El subtotal se calcula solo, no se guarda aparte.
        public decimal Subtotal
        {
            get { return Cantidad * PrecioUnitario; }
        }

        // ---- Constructor ----
        public LineaPedido(string codigoProducto, string nombreProducto, int cantidad, decimal precioUnitario)
        {
            if (string.IsNullOrWhiteSpace(codigoProducto))
                throw new ArgumentException("El código del producto no puede estar vacío.");

            if (cantidad <= 0)
                throw new ArgumentException("La cantidad debe ser mayor que cero.");

            if (precioUnitario < 0)
                throw new ArgumentException("El precio unitario no puede ser negativo.");

            CodigoProducto = codigoProducto.Trim();
            NombreProducto = nombreProducto ?? "";
            Cantidad = cantidad;
            PrecioUnitario = precioUnitario;
            CodigoSedeSuministra = "";
        }

        // ---- Asignar la sede que entrega este producto ----
        public void AsignarSede(string codigoSede)
        {
            if (string.IsNullOrWhiteSpace(codigoSede))
                throw new ArgumentException("El código de la sede no puede estar vacío.");
            CodigoSedeSuministra = codigoSede.Trim();
        }

        // ---- Texto para mostrar la línea ----
        public override string ToString()
        {
            return $"{NombreProducto} x{Cantidad} @ ₡{PrecioUnitario} = ₡{Subtotal} (sede: {CodigoSedeSuministra})";
        }
    }
}