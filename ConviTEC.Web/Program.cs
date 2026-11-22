using ConviTEC.Core.Logica;
using ConviTEC.Core.Persistencia;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
string datos = config["Datos"] ?? Path.Combine(builder.Environment.ContentRootPath, "..", "datos");
await using var sede = new ServicioSede(new OpcionesSede
{
    CodigoSede = config["Sede"] ?? throw new ArgumentException("Indique --Sede S1, S2, S3 o S4."),
    CarpetaDatos = datos,
    HostRabbit = config["Rabbit:Host"] ?? "localhost",
    UsuarioRabbit = config["Rabbit:Usuario"] ?? "guest",
    ClaveRabbit = config["Rabbit:Clave"] ?? "guest"
});
var grafo = CargadorGrafo.CargarDesdeArchivo(Path.Combine(datos, "rutas.json"));
await sede.IniciarAsync();
var pedidos = new ServicioPedidos(sede, grafo);
await pedidos.IniciarAsync();
var entregas = new ServicioEntregas(pedidos, sede);
builder.Services.AddSingleton(entregas);
builder.Services.AddHostedService<HorarioEntregas>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/sede", () => sede.Sede);
app.MapGet("/api/destinos", () => grafo.Vertices());
app.MapGet("/api/inventario", () => sede.ListarInventarioLocal().ToArray());
app.MapGet("/api/catalogo", () => sede.CatalogoGlobal().ToArray());
app.MapPost("/api/inventario/{codigo}/unidades", async (string codigo, Unidades solicitud) =>
{
    if (solicitud.Cantidad <= 0) return Results.BadRequest(new { mensaje = "La cantidad debe ser positiva." });
    var item = sede.ConsultarLocal(codigo);
    if (item == null) return Results.NotFound(new { mensaje = "Producto inexistente en esta sede." });
    await sede.AgregarUnidadesAsync(codigo, solicitud.Cantidad);
    return Results.Ok(new { mensaje = "Unidades agregadas." });
});
app.MapGet("/api/pedidos", () => pedidos.ObtenerHistorial().ToArray());
app.MapGet("/api/pedidos/pendientes", () => pedidos.ObtenerPendientes().ToArray());
app.MapPost("/api/pedidos", async (SolicitudPedido solicitud) =>
{
    var resultado = await pedidos.RegistrarPedidoAsync(solicitud.Lineas, solicitud.Destino);
    return resultado.Aceptado ? Results.Ok(resultado) : Results.BadRequest(resultado);
});
app.MapGet("/api/ventas", () => pedidos.ObtenerVentas().ToArray());
app.MapGet("/api/estadisticas/estrella", () => Estadisticas.Estrellas(pedidos.ObtenerVentas()).ToArray());
app.MapGet("/api/estadisticas/peores", () => Estadisticas.Peores(pedidos.ObtenerHistorial()));
app.MapGet("/api/entregas", entregas.Estado);
app.MapPost("/api/entregas/siguiente", entregas.Siguiente);
await app.RunAsync();

record Unidades(int Cantidad);
record SolicitudPedido(LineaSolicitada[] Lineas, string Destino);
