namespace ConviTEC.Core.Modelos
{
    /// <summary>
    /// Un producto dentro del inventario de una sede,
    /// con su cantidad disponible y su cantidad reservada.
    /// </summary>
    public class ItemInventario
    {
        public Producto Producto { get; private set; }
        public int Disponible { get; private set; }   // unidades libres
        public int Reservado { get; private set; }    // unidades apartadas para pedidos

        public int Total
        {
            get { return Disponible + Reservado; }
        }

        public ItemInventario(Producto producto, int cantidadInicial)
        {
            if (producto == null)
                throw new ArgumentException("El producto no puede ser nulo.");
            if (cantidadInicial < 0)
                throw new ArgumentException("La cantidad inicial no puede ser negativa.");

            Producto = producto;
            Disponible = cantidadInicial;
            Reservado = 0;
        }

        // ---- Agregar unidades nuevas (llegó mercadería) ----
        public void AgregarUnidades(int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad a agregar debe ser mayor que cero.");
            Disponible += cantidad;
        }

        // ---- Apartar unidades para un pedido ----
        public void Reservar(int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad a reservar debe ser mayor que cero.");
            if (cantidad > Disponible)
                throw new InvalidOperationException(
                    $"No hay suficiente {Producto.Nombre}: se piden {cantidad}, hay {Disponible} disponibles.");

            Disponible -= cantidad;
            Reservado += cantidad;
        }

        // ---- Devolver unidades reservadas (pedido cancelado) ----
        public void LiberarReserva(int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad a liberar debe ser mayor que cero.");
            if (cantidad > Reservado)
                throw new InvalidOperationException(
                    $"No se pueden liberar {cantidad} de {Producto.Nombre}: solo hay {Reservado} reservadas.");

            Reservado -= cantidad;
            Disponible += cantidad;
        }

        // ---- Confirmar: las unidades reservadas salen del inventario ----
        public void ConfirmarReserva(int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad a confirmar debe ser mayor que cero.");
            if (cantidad > Reservado)
                throw new InvalidOperationException(
                    $"No se pueden confirmar {cantidad} de {Producto.Nombre}: solo hay {Reservado} reservadas.");

            Reservado -= cantidad;
        }

        public override string ToString()
        {
            return $"{Producto} | Disponible: {Disponible} | Reservado: {Reservado}";
        }
    }
}