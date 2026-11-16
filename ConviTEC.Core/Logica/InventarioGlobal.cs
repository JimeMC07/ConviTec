using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Modelos;

namespace ConviTEC.Core.Logica
{
    /// <summary>
    /// Copia local del inventario de TODAS las sedes.
    /// Solo se actualiza con la información que llega por mensajes.
    /// </summary>
    public class InventarioGlobal
    {
        private ArbolAVL<EntradaCatalogo> catalogo;

        public int CantidadProductos
        {
            get { return catalogo.Cantidad; }
        }

        public InventarioGlobal()
        {
            catalogo = new ArbolAVL<EntradaCatalogo>();
        }

        // ---- Actualizar cuánto tiene una sede de un producto ----
        // Si el producto no estaba en el catálogo, se agrega.
        public void Actualizar(string codigoSede, Producto producto, int cantidad)
        {
            if (producto == null)
                throw new ArgumentException("El producto no puede ser nulo.");

            EntradaCatalogo? entrada = catalogo.Buscar(producto.Codigo);
            if (entrada == null)
            {
                entrada = new EntradaCatalogo(producto);
                catalogo.Insertar(producto.Codigo, entrada);
            }
            entrada.ActualizarCantidad(codigoSede, cantidad);
        }

        // ---- Consultar un producto (null si no existe) ----
        public EntradaCatalogo? Consultar(string codigoProducto)
        {
            return catalogo.Buscar(codigoProducto);
        }

        public bool ExisteProducto(string codigoProducto)
        {
            return catalogo.Contiene(codigoProducto);
        }

        // ---- Cuánto tiene una sede de un producto ----
        public int CantidadEn(string codigoProducto, string codigoSede)
        {
            EntradaCatalogo? entrada = catalogo.Buscar(codigoProducto);
            return entrada == null ? 0 : entrada.CantidadEn(codigoSede);
        }

        // ---- Sedes que tienen al menos 1 unidad del producto ----
        public Lista<ExistenciaSede> SedesConDisponibilidad(string codigoProducto)
        {
            Lista<ExistenciaSede> resultado = new Lista<ExistenciaSede>();
            EntradaCatalogo? entrada = catalogo.Buscar(codigoProducto);
            if (entrada == null) return resultado;   // vacía si no existe

            foreach (ExistenciaSede e in entrada.Existencias)
                if (e.Cantidad > 0)
                    resultado.Agregar(e);

            return resultado;
        }

        // ---- Todo el catálogo ordenado por código ----
        public Lista<EntradaCatalogo> ListarCatalogo()
        {
            return catalogo.EnOrden();
        }
    }
}