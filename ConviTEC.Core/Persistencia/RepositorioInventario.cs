using System.Text.Encodings.Web;
using System.Text.Json;
using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Logica;
using ConviTEC.Core.Modelos;

namespace ConviTEC.Core.Persistencia
{
    /// <summary>
    /// Guarda y carga el inventario local de una sede en un archivo JSON.
    /// </summary>
    public class RepositorioInventario
    {
        private readonly string rutaArchivo;

        // Opciones del JSON: con sangría (bonito) y que respete tildes (José, Limón)
        private static readonly JsonSerializerOptions opciones = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // carpetaDatos = carpeta propia de la sede, por ejemplo "datos/S3"
        public RepositorioInventario(string carpetaDatos)
        {
            if (string.IsNullOrWhiteSpace(carpetaDatos))
                throw new ArgumentException("La carpeta de datos no puede estar vacía.");

            Directory.CreateDirectory(carpetaDatos);   // la crea si no existe
            rutaArchivo = Path.Combine(carpetaDatos, "inventario.json");
        }

        // ---- GUARDAR ----
        public void Guardar(InventarioLocal inventario)
        {
            if (inventario == null)
                throw new ArgumentException("El inventario no puede ser nulo.");

            // 1. Árbol → arreglo de items
            ItemInventario[] items = inventario.ListarTodo().ToArray();

            // 2. Items → arreglo de DTOs
            ItemInventarioDTO[] dtos = new ItemInventarioDTO[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                dtos[i] = new ItemInventarioDTO
                {
                    Codigo = items[i].Producto.Codigo,
                    Nombre = items[i].Producto.Nombre,
                    Descripcion = items[i].Producto.Descripcion,
                    Categoria = items[i].Producto.Categoria,
                    Precio = items[i].Producto.Precio,
                    Disponible = items[i].Disponible,
                    Reservado = items[i].Reservado
                };
            }

            // 3. DTOs → texto JSON → archivo
            // Se escribe primero a un archivo temporal y luego se reemplaza,
            // así si el programa se cae a la mitad no se daña el archivo bueno.
            string json = JsonSerializer.Serialize(dtos, opciones);
            string rutaTemporal = rutaArchivo + ".tmp";
            File.WriteAllText(rutaTemporal, json);
            File.Move(rutaTemporal, rutaArchivo, true);
        }

        // ---- CARGAR ----
        public InventarioLocal Cargar(string codigoSede)
        {
            InventarioLocal inventario = new InventarioLocal(codigoSede);

            // Primera vez: no hay archivo todavía → inventario vacío
            if (!File.Exists(rutaArchivo))
                return inventario;

            ItemInventarioDTO[]? dtos;
            try
            {
                string json = File.ReadAllText(rutaArchivo);
                dtos = JsonSerializer.Deserialize<ItemInventarioDTO[]>(json, opciones);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"El archivo {rutaArchivo} está dañado o mal escrito: {ex.Message}");
            }

            if (dtos == null) return inventario;

            // Reconstruir el árbol insertando cada producto
            for (int i = 0; i < dtos.Length; i++)
            {
                ItemInventarioDTO d = dtos[i];
                Producto producto = new Producto(d.Codigo, d.Nombre, d.Descripcion, d.Categoria, d.Precio);

                // Las reservas que quedaron a medias al cerrar se devuelven a disponible
                int cantidad = d.Disponible + d.Reservado;

                if (!inventario.ExisteProducto(producto.Codigo))   // ignora duplicados en el archivo
                    inventario.RegistrarProducto(producto, cantidad);
            }

            return inventario;
        }
    }
}