using System.Text.Json;
using ConviTEC.Core.Rutas;

namespace ConviTEC.Core.Persistencia
{
    /// <summary>Forma plana de una ruta, tal como viene en rutas.json.</summary>
    public class RutaDTO
    {
        public string Origen { get; set; } = "";
        public string Destino { get; set; } = "";
        public string Ruta { get; set; } = "";
        public int Tiempo { get; set; }
    }

    /// <summary>
    /// Lee el archivo de rutas y construye el grafo.
    /// </summary>
    public static class CargadorGrafo
    {
        private static readonly JsonSerializerOptions opciones = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true   // acepta "origen" u "Origen"
        };

        public static Grafo CargarDesdeArchivo(string ruta)
        {
            if (!File.Exists(ruta))
                throw new FileNotFoundException($"No se encontró el archivo de rutas: {ruta}");

            RutaDTO[]? rutas;
            try
            {
                rutas = JsonSerializer.Deserialize<RutaDTO[]>(File.ReadAllText(ruta), opciones);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"El archivo de rutas está dañado o mal escrito: {ex.Message}");
            }

            Grafo grafo = new Grafo();
            if (rutas == null) return grafo;   // archivo vacío → grafo vacío

            for (int i = 0; i < rutas.Length; i++)
            {
                RutaDTO r = rutas[i];
                if (string.IsNullOrWhiteSpace(r.Origen) || string.IsNullOrWhiteSpace(r.Destino))
                    throw new InvalidDataException($"La ruta {r.Ruta} no tiene origen o destino.");

                grafo.AgregarRuta(r.Origen, r.Destino, r.Tiempo);   // si es duplicada, se ignora
            }
            return grafo;
        }
    }
}