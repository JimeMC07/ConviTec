using System.Text.Json;
using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Logica;
using ConviTEC.Core.Modelos;

namespace ConviTEC.Core.Persistencia
{
    /// <summary>Forma plana de una sede en sedes.json.</summary>
    public class SedeDTO
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Ubicacion { get; set; } = "";
    }

    /// <summary>Un producto del inventario inicial, indicando a qué sede pertenece.</summary>
    public class ItemInicialDTO : ItemInventarioDTO
    {
        public string Sede { get; set; } = "";
    }

    /// <summary>
    /// Lee los archivos de configuración: las sedes y el inventario inicial.
    /// </summary>
    public static class CargadorConfiguracion
    {
        private static readonly JsonSerializerOptions opciones = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // ---- Leer todas las sedes ----
        public static Lista<Sede> CargarSedes(string ruta)
        {
            SedeDTO[] dtos = LeerArreglo<SedeDTO>(ruta);
            Lista<Sede> sedes = new Lista<Sede>();

            for (int i = 0; i < dtos.Length; i++)
            {
                SedeDTO d = dtos[i];
                if (sedes.Existe(s => s.Codigo == d.Codigo))
                    throw new InvalidDataException($"El código de sede {d.Codigo} está repetido en {ruta}.");

                sedes.Agregar(new Sede(d.Codigo, d.Nombre, d.Ubicacion));
            }

            if (sedes.EstaVacia())
                throw new InvalidDataException($"El archivo {ruta} no tiene ninguna sede.");

            return sedes;
        }

        // ---- Crear el inventario inicial de UNA sede ----
        public static InventarioLocal CargarInventarioInicial(string ruta, string codigoSede)
        {
            InventarioLocal inventario = new InventarioLocal(codigoSede);
            if (!File.Exists(ruta)) return inventario;   // sin archivo: empieza vacío

            ItemInicialDTO[] dtos = LeerArreglo<ItemInicialDTO>(ruta);
            for (int i = 0; i < dtos.Length; i++)
            {
                ItemInicialDTO d = dtos[i];
                if (d.Sede != codigoSede) continue;                     // solo los de esta sede
                if (inventario.ExisteProducto(d.Codigo)) continue;      // ignora repetidos

                Producto producto = new Producto(d.Codigo, d.Nombre, d.Descripcion, d.Categoria, d.Precio);
                inventario.RegistrarProducto(producto, d.Disponible);
            }
            return inventario;
        }

        // ---- Ayudante: leer un arreglo de un archivo JSON ----
        private static T[] LeerArreglo<T>(string ruta)
        {
            if (!File.Exists(ruta))
                throw new FileNotFoundException($"No se encontró el archivo: {ruta}");
            try
            {
                T[]? datos = JsonSerializer.Deserialize<T[]>(File.ReadAllText(ruta), opciones);
                return datos ?? new T[0];
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"El archivo {ruta} está dañado o mal escrito: {ex.Message}");
            }
        }
    }
}