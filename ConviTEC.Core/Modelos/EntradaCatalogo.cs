using ConviTEC.Core.Estructuras;

namespace ConviTEC.Core.Modelos
{
    /// <summary>
    /// Cuántas unidades de un producto tiene una sede.
    /// </summary>
    public class ExistenciaSede
    {
        public string CodigoSede { get; private set; }
        public int Cantidad { get; private set; }

        public ExistenciaSede(string codigoSede, int cantidad)
        {
            if (string.IsNullOrWhiteSpace(codigoSede))
                throw new ArgumentException("El código de la sede no puede estar vacío.");
            if (cantidad < 0)
                throw new ArgumentException("La cantidad no puede ser negativa.");

            CodigoSede = codigoSede.Trim();
            Cantidad = cantidad;
        }

        public void CambiarCantidad(int nuevaCantidad)
        {
            if (nuevaCantidad < 0)
                throw new ArgumentException("La cantidad no puede ser negativa.");
            Cantidad = nuevaCantidad;
        }

        public override string ToString()
        {
            return $"{CodigoSede}: {Cantidad}";
        }
    }

    /// <summary>
    /// Una fila del catálogo global: un producto y cuánto tiene cada sede.
    /// </summary>
    public class EntradaCatalogo
    {
        public Producto Producto { get; private set; }
        public Lista<ExistenciaSede> Existencias { get; private set; }

        // Suma de todas las sedes
        public int TotalGlobal
        {
            get
            {
                int total = 0;
                foreach (ExistenciaSede e in Existencias)
                    total += e.Cantidad;
                return total;
            }
        }

        public EntradaCatalogo(Producto producto)
        {
            if (producto == null)
                throw new ArgumentException("El producto no puede ser nulo.");
            Producto = producto;
            Existencias = new Lista<ExistenciaSede>();
        }

        // ---- Cantidad en una sede (0 si esa sede no lo tiene) ----
        public int CantidadEn(string codigoSede)
        {
            ExistenciaSede? e = Existencias.Buscar(x => x.CodigoSede == codigoSede);
            return e == null ? 0 : e.Cantidad;
        }

        // ---- Poner la cantidad de una sede (la crea si no estaba) ----
        public void ActualizarCantidad(string codigoSede, int cantidad)
        {
            ExistenciaSede? e = Existencias.Buscar(x => x.CodigoSede == codigoSede);
            if (e == null)
                Existencias.Agregar(new ExistenciaSede(codigoSede, cantidad));
            else
                e.CambiarCantidad(cantidad);
        }

        public override string ToString()
        {
            string texto = $"{Producto.Codigo} {Producto.Nombre} | ";
            foreach (ExistenciaSede e in Existencias)
                texto += e + "  ";
            return texto + $"| Total: {TotalGlobal}";
        }
    }
}