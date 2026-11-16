namespace ConviTEC.Core.Modelos
{
    /// <summary>
    /// Representa un producto que vende ConviTEC.
    /// </summary>
    public class Producto
    {
        // ---- Datos del producto ----
        public string Codigo { get; private set; }
        public string Nombre { get; private set; }
        public string Descripcion { get; private set; }
        public string Categoria { get; private set; }
        public decimal Precio { get; private set; }

        // ---- Constructor: se usa para crear un producto nuevo ----
        public Producto(string codigo, string nombre, string descripcion, string categoria, decimal precio)
        {
            // Validaciones: no dejamos crear productos con datos inválidos
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código del producto no puede estar vacío.");

            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del producto no puede estar vacío.");

            if (precio < 0)
                throw new ArgumentException("El precio no puede ser negativo.");

            Codigo = codigo.Trim();
            Nombre = nombre.Trim();
            Descripcion = descripcion ?? "";
            Categoria = categoria ?? "Sin categoría";
            Precio = precio;
        }

        // ---- Permite cambiar el precio después ----
        public void CambiarPrecio(decimal nuevoPrecio)
        {
            if (nuevoPrecio < 0)
                throw new ArgumentException("El precio no puede ser negativo.");
            Precio = nuevoPrecio;
        }

        // ---- Texto para mostrar el producto (útil para pruebas) ----
        public override string ToString()
        {
            return $"[{Codigo}] {Nombre} - {Categoria} - ₡{Precio}";
        }
    }
}