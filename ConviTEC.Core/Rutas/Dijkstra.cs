using ConviTEC.Core.Estructuras;

namespace ConviTEC.Core.Rutas
{
    /// <summary>
    /// Resultado de correr Dijkstra desde un origen:
    /// el tiempo mínimo a cada lugar y el "anterior" de cada uno (para armar el camino).
    /// </summary>
    public class ResultadoDijkstra
    {
        public const int INFINITO = int.MaxValue;

        private readonly Grafo grafo;
        private readonly int[] distancias;   // distancias[i] = minutos desde el origen hasta i
        private readonly int[] anteriores;   // anteriores[i] = de qué lugar se llegó a i (-1 = ninguno)

        public string Origen { get; private set; }

        public ResultadoDijkstra(Grafo grafo, string origen, int[] distancias, int[] anteriores)
        {
            this.grafo = grafo;
            Origen = origen;
            this.distancias = distancias;
            this.anteriores = anteriores;
        }

        // ---- ¿Se puede llegar a ese destino? ----
        public bool EsAlcanzable(string destino)
        {
            int j = grafo.IndiceDe(destino);
            return j != -1 && distancias[j] != INFINITO;
        }

        // ---- Minutos hasta el destino (-1 si es inalcanzable) ----
        public int TiempoA(string destino)
        {
            int j = IndiceValido(destino);
            return distancias[j] == INFINITO ? -1 : distancias[j];
        }

        // ---- Camino completo desde el origen hasta el destino ----
        // Lista vacía si es inalcanzable.
        public Lista<string> CaminoA(string destino)
        {
            Lista<string> camino = new Lista<string>();
            int j = IndiceValido(destino);
            if (distancias[j] == INFINITO) return camino;

            // Se recorre hacia atrás siguiendo los "anteriores"
            int actual = j;
            while (actual != -1)
            {
                camino.AgregarAlInicio(grafo.NombreDe(actual));
                actual = anteriores[actual];
            }
            return camino;
        }

        private int IndiceValido(string nombre)
        {
            int j = grafo.IndiceDe(nombre);
            if (j == -1)
                throw new ArgumentException($"La ubicación '{nombre}' no existe en el grafo.");
            return j;
        }
    }

    /// <summary>
    /// Algoritmo de Dijkstra implementado por nosotros (sin bibliotecas de grafos).
    /// Calcula el tiempo mínimo desde un origen hacia todos los vértices.
    /// </summary>
    public static class Dijkstra
    {
        public static ResultadoDijkstra Calcular(Grafo grafo, string origen)
        {
            if (grafo == null)
                throw new ArgumentException("El grafo no puede ser nulo.");

            int indiceOrigen = grafo.IndiceDe(origen);
            if (indiceOrigen == -1)
                throw new ArgumentException($"El origen '{origen}' no existe en el grafo.");

            int n = grafo.CantidadVertices;
            int[] distancias = new int[n];
            int[] anteriores = new int[n];
            bool[] visitados = new bool[n];

            // 1. Todos en infinito, el origen en 0
            for (int i = 0; i < n; i++)
            {
                distancias[i] = ResultadoDijkstra.INFINITO;
                anteriores[i] = -1;
                visitados[i] = false;
            }
            distancias[indiceOrigen] = 0;

            for (int paso = 0; paso < n; paso++)
            {
                // 2. Escoger el NO visitado más cercano
                int u = -1;
                for (int i = 0; i < n; i++)
                {
                    if (!visitados[i] && distancias[i] != ResultadoDijkstra.INFINITO
                        && (u == -1 || distancias[i] < distancias[u]))
                        u = i;
                }

                if (u == -1) break;   // no quedan alcanzables: los demás son inalcanzables

                visitados[u] = true;   // su tiempo ya es definitivo

                // 3. Revisar sus vecinos y mejorar tiempos si se puede
                for (int v = 0; v < n; v++)
                {
                    int peso = grafo.Peso(u, v);
                    if (peso > 0 && !visitados[v])
                    {
                        int nuevaDistancia = distancias[u] + peso;
                        if (nuevaDistancia < distancias[v])
                        {
                            distancias[v] = nuevaDistancia;
                            anteriores[v] = u;
                        }
                    }
                }
            }

            return new ResultadoDijkstra(grafo, origen.Trim(), distancias, anteriores);
        }
    }
}