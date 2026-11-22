using System.Text.Json;
using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Modelos;
using ConviTEC.Core.Rutas;

namespace ConviTEC.Core.Logica;

public record Parada(string Destino, string[] Camino, int MinutosTramo, int MinutosAcumulados, string[] Pedidos);
public record Recorrido(string Origen, DateTime? Horario, Parada[] Paradas, int Visitadas, string[] Restantes)
{
    public int MinutosTotales => Visitadas == 0 ? 0 : Paradas[Visitadas - 1].MinutosAcumulados;
    public bool Terminado => Restantes.Length == 0 && Visitadas == Paradas.Length;
}

public class ServicioEntregas
{
    private readonly ServicioPedidos pedidos;
    private readonly string archivo;
    private readonly object turno = new();
    private Recorrido actual;
    private DateTime? ultimaSalida;

    private record Registro(Recorrido Actual, DateTime? UltimaSalida);

    public ServicioEntregas(ServicioPedidos pedidos, ServicioSede sede)
    {
        this.pedidos = pedidos;
        archivo = Path.Combine(sede.CarpetaSede, "recorrido.json");
        var registro = File.Exists(archivo)
            ? JsonSerializer.Deserialize<Registro>(File.ReadAllText(archivo)) : null;
        actual = registro?.Actual ?? new Recorrido(sede.Sede.Ubicacion, null, [], 0, []);
        ultimaSalida = registro?.UltimaSalida;
    }

    public Recorrido Estado() { lock (turno) return actual; }

    public void ReanudarAutomatica()
    {
        lock (turno)
            if (actual.Horario.HasValue)
                while (!actual.Terminado) Avanzar();
    }

    public static Parada? CalcularParada(Grafo grafo, string origen, Lista<Pedido> restantes, int acumulado)
    {
            if (restantes.EstaVacia()) return null;
            var rutas = Dijkstra.Calcular(grafo, origen);
            Pedido? elegido = null;
            int minimo = int.MaxValue;
            foreach (var pedido in restantes)
            {
                if (!rutas.EsAlcanzable(pedido.Destino)) continue;
                int minutos = rutas.TiempoA(pedido.Destino);
                if (minutos < minimo) { minimo = minutos; elegido = pedido; }
            }
            if (elegido == null) return null;
            string destino = elegido.Destino;
            var ids = new Lista<string>();
            foreach (var pedido in restantes)
                if (pedido.Destino == destino) ids.Agregar(pedido.Id);
            return new(destino, rutas.CaminoA(destino).ToArray(), minimo, checked(acumulado + minimo), ids.ToArray());
    }

    public Recorrido Siguiente()
    {
        lock (turno)
        {
            if (actual.Terminado && !pedidos.ObtenerPendientes().EstaVacia()) Iniciar(null);
            Avanzar();
            return actual;
        }
    }

    public void Despachar(DateTime horario)
    {
        lock (turno)
        {
            if (ultimaSalida >= horario) return;
            while (!actual.Terminado) Avanzar();
            Iniciar(horario);
            while (!actual.Terminado) Avanzar();
        }
    }

    private void Iniciar(DateTime? horario)
    {
        var ids = new Lista<string>();
        foreach (var pedido in pedidos.ObtenerPendientes()) ids.Agregar(pedido.Id);
        Guardar(new(actual.Origen, horario, [], 0, ids.ToArray()), horario ?? ultimaSalida);
    }

    private void Avanzar()
    {
        if (actual.Terminado) return;
        if (actual.Visitadas == actual.Paradas.Length)
        {
            var pendientes = new Lista<Pedido>();
            foreach (string id in actual.Restantes)
                if (pedidos.ConsultarPedido(id) is { Estado: EstadoPedido.PENDIENTE } pedido)
                    pendientes.Agregar(pedido);
            string origen = actual.Visitadas == 0 ? actual.Origen : actual.Paradas[^1].Destino;
            var proxima = CalcularParada(pedidos.Grafo, origen, pendientes, actual.MinutosTotales);
            if (proxima == null)
            {
                Guardar(actual with { Restantes = [] }, ultimaSalida);
                return;
            }
            var restantes = new Lista<string>();
            foreach (var pedido in pendientes)
                if (pedido.Destino != proxima.Destino) restantes.Agregar(pedido.Id);
            Guardar(actual with { Paradas = [.. actual.Paradas, proxima], Restantes = restantes.ToArray() }, ultimaSalida);
        }
        var parada = actual.Paradas[actual.Visitadas];
        foreach (string id in parada.Pedidos)
            if (pedidos.ConsultarPedido(id)?.Estado == EstadoPedido.PENDIENTE)
                pedidos.MarcarEntregado(id, parada.MinutosAcumulados);
        Guardar(actual with { Visitadas = actual.Visitadas + 1 }, ultimaSalida);
    }

    private void Guardar(Recorrido nuevo, DateTime? horario)
    {
        File.WriteAllText(archivo + ".tmp", JsonSerializer.Serialize(new Registro(nuevo, horario)));
        File.Move(archivo + ".tmp", archivo, true);
        actual = nuevo;
        ultimaSalida = horario;
    }
}
