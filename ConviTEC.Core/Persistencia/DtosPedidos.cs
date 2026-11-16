using ConviTEC.Core.Modelos;

namespace ConviTEC.Core.Persistencia
{
    /// <summary>Forma plana de una línea (para pedidos y ventas).</summary>
    public class LineaDTO
    {
        public string CodigoProducto { get; set; } = "";
        public string NombreProducto { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string CodigoSedeSuministra { get; set; } = "";

        // LineaPedido → DTO
        public static LineaDTO Desde(LineaPedido linea)
        {
            return new LineaDTO
            {
                CodigoProducto = linea.CodigoProducto,
                NombreProducto = linea.NombreProducto,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario,
                CodigoSedeSuministra = linea.CodigoSedeSuministra
            };
        }

        // DTO → LineaPedido
        public LineaPedido ALinea()
        {
            LineaPedido linea = new LineaPedido(CodigoProducto, NombreProducto, Cantidad, PrecioUnitario);
            if (!string.IsNullOrWhiteSpace(CodigoSedeSuministra))
                linea.AsignarSede(CodigoSedeSuministra);
            return linea;
        }

        // Convierte las líneas de un pedido/venta en un arreglo de DTOs
        public static LineaDTO[] DesdeLineas(LineaPedido[] lineas)
        {
            LineaDTO[] dtos = new LineaDTO[lineas.Length];
            for (int i = 0; i < lineas.Length; i++)
                dtos[i] = Desde(lineas[i]);
            return dtos;
        }
    }

    /// <summary>Forma plana de un pedido.</summary>
    public class PedidoDTO
    {
        public string Id { get; set; } = "";
        public DateTime FechaCreacion { get; set; }
        public string Estado { get; set; } = "";
        public string CodigoSedeOrigen { get; set; } = "";
        public string Destino { get; set; } = "";
        public string Motivo { get; set; } = "";
        public int TiempoTraslado { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public LineaDTO[] Lineas { get; set; } = new LineaDTO[0];
        public string[] SedesParticipantes { get; set; } = new string[0];
    }

    /// <summary>Forma plana de una venta.</summary>
    public class VentaDTO
    {
        public string Id { get; set; } = "";
        public string IdPedido { get; set; } = "";
        public string CodigoSede { get; set; } = "";
        public DateTime Fecha { get; set; }
        public LineaDTO[] Lineas { get; set; } = new LineaDTO[0];
    }
}