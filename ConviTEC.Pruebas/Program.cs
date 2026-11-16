using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Logica;
using ConviTEC.Core.Modelos;
using ConviTEC.Core.Persistencia;
using ConviTEC.Core.Rutas;

int aprobadas = 0;
int fallidas = 0;

// ---------- Ayudantes para las pruebas ----------
void Verificar(bool condicion, string nombre)
{
    if (condicion)
    {
        aprobadas++;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  [OK]    " + nombre);
    }
    else
    {
        fallidas++;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  [FALLA] " + nombre);
    }
    Console.ResetColor();
}

// Devuelve true si la acción lanza un error (para probar casos inválidos)
bool LanzaError(Action accion)
{
    try { accion(); return false; }
    catch (Exception) { return true; }
}

void Titulo(string texto)
{
    Console.WriteLine();
    Console.WriteLine("===== " + texto + " =====");
}

// =====================================================
Titulo("LISTA ENLAZADA");
Lista<int> lista = new Lista<int>();
Verificar(lista.EstaVacia(), "Una lista nueva está vacía");
lista.Agregar(10); lista.Agregar(20); lista.Agregar(30);
Verificar(lista.Cantidad == 3, "Agregar 3 elementos");
Verificar(lista.Obtener(1) == 20, "Obtener(1) devuelve 20");
Verificar(lista.Eliminar(x => x == 20) && lista.Cantidad == 2, "Eliminar el 20");
Verificar(!lista.Eliminar(x => x == 99), "Eliminar algo que no existe devuelve false");
Verificar(LanzaError(() => lista.Obtener(5)), "Índice fuera de rango da error");
Verificar(LanzaError(() => new Lista<int>().Obtener(0)), "Obtener en lista vacía da error");

// =====================================================
Titulo("COLA");
Cola<string> cola = new Cola<string>();
Verificar(LanzaError(() => cola.Desencolar()), "Desencolar una cola vacía da error");
cola.Encolar("A"); cola.Encolar("B"); cola.Encolar("C");
Verificar(cola.Desencolar() == "A", "FIFO: el primero en salir es A");
Verificar(cola.Eliminar(x => x == "C") && cola.Cantidad == 1, "Eliminar C aunque no esté al frente");
Verificar(cola.VerFrente() == "B", "El frente ahora es B");

// =====================================================
Titulo("ÁRBOL AVL");
ArbolAVL<string> arbol = new ArbolAVL<string>();
Verificar(arbol.Buscar("X") == null, "Buscar en árbol vacío devuelve null");
for (int i = 1; i <= 100; i++)
    arbol.Insertar("P" + i.ToString("D3"), "Producto " + i);
Verificar(arbol.Cantidad == 100, "Insertar 100 productos");
Verificar(arbol.AlturaArbol() <= 8, $"100 inserciones en orden quedan balanceadas (altura {arbol.AlturaArbol()})");
Verificar(arbol.Buscar("P050") == "Producto 50", "Buscar P050");
Verificar(!arbol.Insertar("P050", "otro"), "No permite claves duplicadas");
Verificar(arbol.Eliminar("P050") && !arbol.Contiene("P050"), "Eliminar P050");
Verificar(!arbol.Eliminar("P999"), "Eliminar algo que no existe devuelve false");
Lista<string> ordenados = arbol.EnOrden();
Verificar(ordenados.Cantidad == 99 && ordenados.Obtener(0) == "Producto 1", "EnOrden devuelve todo ordenado");

// =====================================================
Titulo("INVENTARIO LOCAL");
InventarioLocal inventario = new InventarioLocal("S3");
Producto coca = new Producto("P001", "Coca Cola", "Lata 355ml", "Bebidas", 800);
inventario.RegistrarProducto(coca, 10);
Verificar(LanzaError(() => inventario.RegistrarProducto(coca, 5)), "No registra un producto duplicado");
inventario.Reservar("P001", 3);
ItemInventario item = inventario.Consultar("P001")!;
Verificar(item.Disponible == 7 && item.Reservado == 3, "Reservar 3: disponible 7, reservado 3");
Verificar(LanzaError(() => inventario.Reservar("P001", 50)), "No deja reservar más de lo disponible");
inventario.LiberarReserva("P001", 1);
Verificar(item.Disponible == 8 && item.Reservado == 2, "Liberar 1: disponible 8, reservado 2");
inventario.ConfirmarReserva("P001", 2);
Verificar(item.Disponible == 8 && item.Reservado == 0, "Confirmar 2: salen del inventario");
inventario.AgregarUnidades("P001", 5);
Verificar(item.Disponible == 13, "Agregar 5 unidades: disponible 13");
Verificar(LanzaError(() => inventario.Reservar("NOEXISTE", 1)), "Reservar un producto inexistente da error");

// =====================================================
Titulo("INVENTARIO GLOBAL");
InventarioGlobal global = new InventarioGlobal();
global.Actualizar("S1", coca, 5);
global.Actualizar("S3", coca, 12);
Verificar(global.CantidadEn("P001", "S3") == 12, "San José tiene 12 de P001");
Verificar(global.Consultar("P001")!.TotalGlobal == 17, "Total global de P001 es 17");
global.Actualizar("S1", coca, 0);
Verificar(global.SedesConDisponibilidad("P001").Cantidad == 1, "Solo 1 sede con disponibilidad");
Verificar(global.SedesConDisponibilidad("NOEXISTE").EstaVacia(), "Producto inexistente: lista vacía");

// =====================================================
Titulo("PEDIDO Y VENTA");
Pedido pedido = new Pedido("P-0001", "S3", "Cartago", DateTime.Now);
pedido.AgregarLinea(new LineaPedido("P001", "Coca Cola", 3, 800));
Verificar(LanzaError(() => pedido.AgregarLinea(new LineaPedido("P001", "Coca Cola", 1, 800))), "No permite el mismo producto dos veces");
Verificar(pedido.MontoTotal == 2400, "Monto total = 3 x 800 = 2400");
Verificar(LanzaError(() => pedido.MarcarEntregado(10, DateTime.Now)), "No se puede entregar sin confirmar");
pedido.Confirmar();
pedido.MarcarEntregado(45, DateTime.Now);
Verificar(pedido.Estado == EstadoPedido.ENTREGADO && pedido.TiempoTraslado == 45, "Confirmar y entregar");
Venta venta = Venta.DesdePedido(pedido);
Verificar(venta.MontoTotal == 2400 && venta.IdPedido == "P-0001", "La venta se crea desde el pedido");

// =====================================================
Titulo("PERSISTENCIA");
string carpeta = Path.Combine(Path.GetTempPath(), "convitec_prueba_" + Guid.NewGuid().ToString("N"));

RepositorioInventario repoInventario = new RepositorioInventario(carpeta);
repoInventario.Guardar(inventario);
InventarioLocal inventarioCargado = repoInventario.Cargar("S3");
Verificar(inventarioCargado.CantidadDisponible("P001") == 13, "Guardar y cargar el inventario");

RepositorioPedidos repoPedidos = new RepositorioPedidos(carpeta);
Pedido pendiente = new Pedido("P-0002", "S3", "Heredia", DateTime.Now);
pendiente.AgregarLinea(new LineaPedido("P001", "Coca Cola", 2, 800));
pendiente.Confirmar();

Lista<Pedido> historial = new Lista<Pedido>();
historial.Agregar(pedido);
historial.Agregar(pendiente);
repoPedidos.GuardarPedidos(historial);

Lista<Venta> ventas = new Lista<Venta>();
ventas.Agregar(venta);
repoPedidos.GuardarVentas(ventas);

Lista<Pedido> historialCargado = repoPedidos.CargarPedidos();
Verificar(historialCargado.Cantidad == 2, "Se cargan los 2 pedidos");
Verificar(historialCargado.Buscar(p => p.Id == "P-0001")!.Estado == EstadoPedido.ENTREGADO, "P-0001 sigue ENTREGADO");
Verificar(historialCargado.Buscar(p => p.Id == "P-0002")!.Estado == EstadoPedido.PENDIENTE, "P-0002 sigue PENDIENTE");
Verificar(repoPedidos.CargarVentas().Obtener(0).MontoTotal == 2400, "Se carga la venta con su monto");

string carpetaVacia = Path.Combine(carpeta, "vacia");
Verificar(new RepositorioInventario(carpetaVacia).Cargar("S1").CantidadProductos == 0, "Sin archivo: inventario vacío");

File.WriteAllText(Path.Combine(carpeta, "inventario.json"), "{esto no es json");
Verificar(LanzaError(() => repoInventario.Cargar("S3")), "Archivo dañado: da error claro");

Directory.Delete(carpeta, true);   // limpiar los archivos de prueba


// =====================================================
Titulo("GRAFO Y DIJKSTRA");
Grafo grafo = CargadorGrafo.CargarDesdeArchivo(Path.Combine("datos", "rutas.json"));
Verificar(grafo.CantidadVertices == 13, "Se cargan las 13 ubicaciones del JSON");
Verificar(grafo.HayRutaDirecta("Cartago", "San José (Sede)"), "Las rutas son de ida y vuelta");
Verificar(!grafo.AgregarRuta("Cartago", "Paraíso", 15), "Una ruta duplicada se rechaza");
Verificar(LanzaError(() => grafo.AgregarRuta("A", "B", 0)), "Una ruta con tiempo 0 da error");

ResultadoDijkstra desdeSJ = Dijkstra.Calcular(grafo, "San José (Sede)");
Verificar(desdeSJ.TiempoA("San José (Sede)") == 0, "Del origen a sí mismo = 0 min");
Verificar(desdeSJ.TiempoA("Heredia") == 20, "San José → Heredia = 20 min");
Verificar(desdeSJ.TiempoA("Limón (Sede)") == 180, "San José → Limón = 180 min (por Cartago)");
Verificar(desdeSJ.CaminoA("Limón (Sede)").Cantidad == 5, "El camino a Limón pasa por 5 lugares");
Verificar(!desdeSJ.EsAlcanzable("Liberia (Sede)") && desdeSJ.TiempoA("Liberia (Sede)") == -1, "Liberia es inalcanzable desde San José");
Verificar(desdeSJ.CaminoA("Liberia (Sede)").EstaVacia(), "Camino a un inalcanzable: lista vacía");

ResultadoDijkstra desdeLiberia = Dijkstra.Calcular(grafo, "Liberia (Sede)");
Verificar(desdeLiberia.TiempoA("Cañas") == 65 && !desdeLiberia.EsAlcanzable("Limón (Sede)"), "Liberia solo llega a Cañas");
Verificar(LanzaError(() => Dijkstra.Calcular(grafo, "Narnia")), "Un origen inexistente da error");

// =====================================================
Console.WriteLine();
Console.WriteLine($"RESULTADO: {aprobadas} aprobadas, {fallidas} fallidas");