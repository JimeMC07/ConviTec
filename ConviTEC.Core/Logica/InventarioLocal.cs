using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Modelos;

namespace ConviTEC.Core.Logica
{
    /// <summary>
    /// Inventario de UNA sede. Guarda los productos en un árbol AVL
    /// (clave = código del producto) para consultas rápidas.
    /// Solo esta sede puede modificar su propio inventario.
    /// </summary>
    public class InventarioLocal
    {
        private ArbolAVL<ItemInventario> items;

        public string CodigoSede { get; private set; }

        public int CantidadProductos
        {
            get { return items.Cantidad; }
        }

        public InventarioLocal(string codigoSede)
        {
            if (string.IsNullOrWhiteSpace(codigoSede))
                throw new ArgumentException("El código de la sede no puede estar vacío.");

            CodigoSede = codigoSede.Trim();
            items = new ArbolAVL<ItemInventario>();
        }

        // ---- Registrar un producto nuevo en esta sede ----
        public void RegistrarProducto(Producto producto, int cantidadInicial)
        {
            ItemInventario item = new ItemInventario(producto, cantidadInicial);
            bool insertado = items.Insertar(producto.Codigo, item);

            if (!insertado)
                throw new ArgumentException($"El producto {producto.Codigo} ya está registrado en la sede {CodigoSede}.");
        }

        // ---- Consultar un producto (null si no existe) ----
        public ItemInventario? Consultar(string codigoProducto)
        {
            return items.Buscar(codigoProducto);
        }

        public bool ExisteProducto(string codigoProducto)
        {
            return items.Contiene(codigoProducto);
        }

        // ---- Cantidad disponible (0 si el producto no existe) ----
        public int CantidadDisponible(string codigoProducto)
        {
            ItemInventario? item = items.Buscar(codigoProducto);
            return item == null ? 0 : item.Disponible;
        }

        // ---- Operaciones sobre las cantidades ----
        public void AgregarUnidades(string codigoProducto, int cantidad)
        {
            ObtenerOFallar(codigoProducto).AgregarUnidades(cantidad);
        }

        public void Reservar(string codigoProducto, int cantidad)
        {
            ObtenerOFallar(codigoProducto).Reservar(cantidad);
        }

        public void LiberarReserva(string codigoProducto, int cantidad)
        {
            ObtenerOFallar(codigoProducto).LiberarReserva(cantidad);
        }

        public void ConfirmarReserva(string codigoProducto, int cantidad)
        {
            ObtenerOFallar(codigoProducto).ConfirmarReserva(cantidad);
        }

        // ---- Listar todo el inventario ordenado por código ----
        public Lista<ItemInventario> ListarTodo()
        {
            return items.EnOrden();
        }

        // ---- Ayudante privado: busca el producto o da error claro ----
        private ItemInventario ObtenerOFallar(string codigoProducto)
        {
            ItemInventario? item = items.Buscar(codigoProducto);
            if (item == null)
                throw new ArgumentException($"El producto {codigoProducto} no existe en la sede {CodigoSede}.");
            return item;
        }
    }
}