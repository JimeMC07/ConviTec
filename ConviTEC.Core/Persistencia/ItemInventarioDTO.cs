namespace ConviTEC.Core.Persistencia
{
    /// <summary>
    /// Forma "plana" de un producto del inventario, tal como se guarda en el JSON.
    /// Solo tiene datos, sin lógica.
    /// </summary>
    public class ItemInventarioDTO
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal Precio { get; set; }
        public int Disponible { get; set; }
        public int Reservado { get; set; }
    }
}