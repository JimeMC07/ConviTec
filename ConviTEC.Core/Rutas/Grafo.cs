namespace ConviTEC.Core.Rutas
{
    /// <summary>
    /// Grafo no dirigido y ponderado, implementado con una MATRIZ DE ADYACENCIA.
    /// Cada vértice es una ubicación; el peso de cada arista es el tiempo en minutos.
    /// Un 0 en la matriz significa que no hay ruta directa.
    /// </summary>
    public class Grafo
    {
        private int[,] matriz;        // matriz[i, j] = minutos entre i y j (0 = no hay ruta)
        private string[] nombres;     // nombres[i] = nombre del vértice con índice i

        public int CantidadVertices { get; private set; }

        public Grafo(int capacidadInicial = 16)
        {
            if (capacidadInicial < 1) capacidadInicial = 1;
            matriz = new int[capacidadInicial, capacidadInicial];
            nombres = new string[capacidadInicial];
            CantidadVertices = 0;
        }

        // ---- Buscar el índice de un vértice por nombre (-1 si no existe) ----
        public int IndiceDe(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return -1;
            nombre = nombre.Trim();

            for (int i = 0; i < CantidadVertices; i++)
                if (nombres[i] == nombre)
                    return i;
            return -1;
        }

        public bool ExisteVertice(string nombre)
        {
            return IndiceDe(nombre) != -1;
        }

        // ---- Nombre de un vértice a partir de su índice ----
        public string NombreDe(int indice)
        {
            if (indice < 0 || indice >= CantidadVertices)
                throw new IndexOutOfRangeException($"No existe el vértice con índice {indice}.");
            return nombres[indice];
        }

        // ---- Agregar un vértice (si ya existe, devuelve su índice) ----
        public int AgregarVertice(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre de la ubicación no puede estar vacío.");

            int existente = IndiceDe(nombre);
            if (existente != -1) return existente;   // no se duplica

            if (CantidadVertices == nombres.Length)
                Crecer();

            nombres[CantidadVertices] = nombre.Trim();
            CantidadVertices++;
            return CantidadVertices - 1;
        }

        // ---- Agregar una ruta de ida y vuelta ----
        // Devuelve false si esa ruta ya existía (duplicada).
        public bool AgregarRuta(string origen, string destino, int minutos)
        {
            if (minutos <= 0)
                throw new ArgumentException($"El tiempo de la ruta {origen} - {destino} debe ser mayor que cero.");

            int i = AgregarVertice(origen);
            int j = AgregarVertice(destino);

            if (i == j)
                throw new ArgumentException($"Una ruta no puede ir de {origen} a sí mismo.");

            if (matriz[i, j] != 0) return false;   // ruta duplicada

            matriz[i, j] = minutos;   // ida
            matriz[j, i] = minutos;   // vuelta (grafo no dirigido)
            return true;
        }

        // ---- Minutos de la ruta directa entre dos índices (0 si no hay) ----
        public int Peso(int i, int j)
        {
            if (i < 0 || i >= CantidadVertices || j < 0 || j >= CantidadVertices)
                throw new IndexOutOfRangeException("Índice de vértice fuera de rango.");
            return matriz[i, j];
        }

        // ---- ¿Hay ruta directa entre dos nombres? ----
        public bool HayRutaDirecta(string origen, string destino)
        {
            int i = IndiceDe(origen);
            int j = IndiceDe(destino);
            return i != -1 && j != -1 && matriz[i, j] != 0;
        }

        // ---- Todos los nombres (arreglo nativo) ----
        public string[] Vertices()
        {
            string[] copia = new string[CantidadVertices];
            for (int i = 0; i < CantidadVertices; i++)
                copia[i] = nombres[i];
            return copia;
        }

        // ---- Hacer la matriz más grande cuando se llena ----
        private void Crecer()
        {
            int nuevaCapacidad = nombres.Length * 2;
            int[,] nuevaMatriz = new int[nuevaCapacidad, nuevaCapacidad];
            string[] nuevosNombres = new string[nuevaCapacidad];

            for (int i = 0; i < CantidadVertices; i++)
            {
                nuevosNombres[i] = nombres[i];
                for (int j = 0; j < CantidadVertices; j++)
                    nuevaMatriz[i, j] = matriz[i, j];
            }

            matriz = nuevaMatriz;
            nombres = nuevosNombres;
        }
    }
}