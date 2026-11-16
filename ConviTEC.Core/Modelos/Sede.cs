namespace ConviTEC.Core.Modelos
{
    /// <summary>
    /// Representa una de las sedes (tiendas) de ConviTEC.
    /// </summary>
    public class Sede
    {
        // ---- Datos de la sede ----
        public string Codigo { get; private set; }
        public string Nombre { get; private set; }

        // Nombre del punto en el grafo de rutas, por ejemplo "San José (Sede)".
        // Debe escribirse igual que en el archivo de rutas.
        public string Ubicacion { get; private set; }

        // ---- Constructor ----
        public Sede(string codigo, string nombre, string ubicacion)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código de la sede no puede estar vacío.");

            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre de la sede no puede estar vacío.");

            if (string.IsNullOrWhiteSpace(ubicacion))
                throw new ArgumentException("La ubicación de la sede no puede estar vacía.");

            Codigo = codigo.Trim();
            Nombre = nombre.Trim();
            Ubicacion = ubicacion.Trim();
        }

        // ---- Texto para mostrar la sede ----
        public override string ToString()
        {
            return $"[{Codigo}] {Nombre} ({Ubicacion})";
        }
    }
}