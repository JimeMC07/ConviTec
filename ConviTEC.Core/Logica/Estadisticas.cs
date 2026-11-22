using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Modelos;

namespace ConviTEC.Core.Logica;

public class IngresoProducto(string codigo, string nombre)
{
    public string Codigo { get; } = codigo;
    public string Nombre { get; } = nombre;
    public decimal Ingresos { get; internal set; }
}

public record RendimientoPedido(string Id, decimal Monto, int Minutos)
{
    public decimal? Rendimiento => Minutos > 0 ? Monto / Minutos : null;
}

public static class Estadisticas
{
    public static Lista<IngresoProducto> Estrellas(Lista<Venta> ventas)
    {
        var ingresos = new Lista<IngresoProducto>();
        decimal maximo = 0;
        foreach (var venta in ventas)
        foreach (var linea in venta.Lineas)
        {
            var producto = ingresos.Buscar(p => p.Codigo == linea.CodigoProducto);
            if (producto == null)
            {
                producto = new(linea.CodigoProducto, linea.NombreProducto);
                ingresos.Agregar(producto);
            }
            producto.Ingresos += linea.Subtotal;
            maximo = Math.Max(maximo, producto.Ingresos);
        }
        var estrellas = new Lista<IngresoProducto>();
        foreach (var producto in ingresos)
            if (producto.Ingresos == maximo) estrellas.Agregar(producto);
        return estrellas;
    }

    public static RendimientoPedido[] Peores(Lista<Pedido> historial)
    {
        var datos = new Lista<RendimientoPedido>();
        foreach (var pedido in historial)
            if (pedido.Estado == EstadoPedido.ENTREGADO)
                datos.Agregar(new(pedido.Id, pedido.MontoTotal, pedido.TiempoTraslado));
        return Ordenamiento.PorMezcla(datos, (a, b) =>
        {
            if (a.Rendimiento == null) return b.Rendimiento == null ? 0 : 1;
            if (b.Rendimiento == null) return -1;
            return a.Rendimiento.Value.CompareTo(b.Rendimiento.Value);
        });
    }
}
