using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Logica;
using ConviTEC.Core.Modelos;
using ConviTEC.Core.Persistencia;
using ConviTEC.Core.Rutas;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---- Uso: dotnet run --project ConviTEC.Sede -- CODIGO [hostRabbit] [usuario] [clave] ----
string codigo = args.Length > 0 ? args[0] : Preguntar("Código de la sede (S1, S2, S3, S4): ");
string host = args.Length > 1 ? args[1] : "localhost";
string usuario = args.Length > 2 ? args[2] : "guest";
string clave = args.Length > 3 ? args[3] : "guest";

if (!Directory.Exists("datos"))
{
    Error("No se encontró la carpeta 'datos'. Ejecute el programa desde la carpeta principal del repositorio.");
    return;
}

ServicioSede sede = new ServicioSede(new OpcionesSede
{
    CodigoSede = codigo.Trim().ToUpper(),
    HostRabbit = host,
    UsuarioRabbit = usuario,
    ClaveRabbit = clave
});
ServicioPedidos pedidos;
Grafo grafo;

try
{
    Console.WriteLine("Iniciando sede...");
    await sede.IniciarAsync();
    grafo = CargadorGrafo.CargarDesdeArchivo(Path.Combine("datos", "rutas.json"));
    pedidos = new ServicioPedidos(sede, grafo);
    await pedidos.IniciarAsync();
}
catch (Exception ex)
{
    Error("No se pudo iniciar la sede: " + ex.Message);
    return;
}

Ok($"Sede {sede.Sede.Nombre} ({sede.Sede.Codigo}) lista. RabbitMQ en {host}.");

bool seguir = true;
while (seguir)
{
    Console.WriteLine();
    Console.WriteLine($"===== ConviTEC - {sede.Sede.Nombre} =====");
    Console.WriteLine(" 1. Ver inventario local");
    Console.WriteLine(" 2. Ver catálogo global");
    Console.WriteLine(" 3. Agregar unidades a un producto");
    Console.WriteLine(" 4. Registrar un pedido");
    Console.WriteLine(" 5. Ver pedidos pendientes");
    Console.WriteLine(" 6. Ver historial de pedidos");
    Console.WriteLine(" 7. Ver destinos alcanzables");
    Console.WriteLine(" 0. Salir");
    string opcion = Preguntar("Opción: ");

    try
    {
        switch (opcion)
        {
            case "1":
                foreach (ItemInventario item in sede.ListarInventarioLocal())
                    Console.WriteLine("  " + item);
                break;

            case "2":
                foreach (EntradaCatalogo entrada in sede.CatalogoGlobal())
                    Console.WriteLine("  " + entrada);
                break;

            case "3":
                string cod = Preguntar("Código del producto: ").ToUpper();
                int cant = PreguntarEntero("Cantidad a agregar: ");
                await sede.AgregarUnidadesAsync(cod, cant);
                Ok("Unidades agregadas y avisadas a las demás sedes.");
                break;

            case "4":
                await HacerPedidoAsync();
                break;

            case "5":
                Lista<Pedido> pendientes = pedidos.ObtenerPendientes();
                if (pendientes.EstaVacia()) Console.WriteLine("  No hay pedidos pendientes.");
                foreach (Pedido p in pendientes)
                {
                    Console.WriteLine("  " + p);
                    foreach (LineaPedido l in p.Lineas)
                        Console.WriteLine("      - " + l);
                }
                break;

            case "6":
                Lista<Pedido> historial = pedidos.ObtenerHistorial();
                if (historial.EstaVacia()) Console.WriteLine("  No hay pedidos registrados.");
                foreach (Pedido p in historial)
                    Console.WriteLine("  " + p + (p.Motivo != "" ? "  | " + p.Motivo : ""));
                break;

            case "7":
                ResultadoDijkstra rutas = Dijkstra.Calcular(grafo, sede.Sede.Ubicacion);
                string[] lugares = grafo.Vertices();
                for (int i = 0; i < lugares.Length; i++)
                {
                    int minutos = rutas.TiempoA(lugares[i]);
                    if (minutos > 0)
                        Console.WriteLine($"  {lugares[i]}: {minutos} min");
                }
                break;

            case "0":
                seguir = false;
                break;

            default:
                Error("Opción inválida.");
                break;
        }
    }
    catch (Exception ex)
    {
        Error(ex.Message);   // un error no cierra el programa
    }
}

await sede.DisposeAsync();
Console.WriteLine("Sede cerrada.");

// =====================================================
//  AYUDANTES
// =====================================================

async Task HacerPedidoAsync()
{
    string destino = Preguntar("Punto de entrega (ej. Heredia, Cartago): ");
    Lista<LineaSolicitada> lineas = new Lista<LineaSolicitada>();

    Console.WriteLine("Escriba los productos. Deje el código vacío para terminar.");
    while (true)
    {
        string cod = Preguntar("  Código del producto: ").ToUpper();
        if (cod == "") break;
        int cant = PreguntarEntero("  Cantidad: ");
        lineas.Agregar(new LineaSolicitada(cod, cant));
    }

    Console.WriteLine("Validando ruta y reservando...");
    ResultadoPedido r = await pedidos.RegistrarPedidoAsync(lineas.ToArray(), destino);

    if (r.Aceptado)
    {
        Ok(r.Mensaje);
        foreach (LineaPedido l in r.Pedido!.Lineas)
            Console.WriteLine("    - " + l);
    }
    else
        Error("Pedido rechazado: " + r.Mensaje);
}

string Preguntar(string texto)
{
    Console.Write(texto);
    return (Console.ReadLine() ?? "").Trim();
}

int PreguntarEntero(string texto)
{
    while (true)
    {
        if (int.TryParse(Preguntar(texto), out int numero))
            return numero;
        Error("Debe escribir un número entero.");
    }
}

void Ok(string texto)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(texto);
    Console.ResetColor();
}

void Error(string texto)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(texto);
    Console.ResetColor();
}