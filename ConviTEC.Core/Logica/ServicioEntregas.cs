using System.Text.Json;
using ConviTEC.Core.Estructuras;
using ConviTEC.Core.Modelos;
using ConviTEC.Core.Rutas;

namespace ConviTEC.Core.Logica;

public record Parada(string Destino, string[] Camino, int MinutosTramo, int MinutosAcumulados, string[] Pedidos);
public record Recorrido(string Origen, DateTime? Horario, Parada[] Paradas, int Visitadas)
{
    public int MinutosTotales => Visitadas == 0 ? 0 : Paradas[Visitadas - 1].MinutosAcumulados;
    public bool Terminado => Visitadas == Paradas.Length;
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
        actual = registro?.Actual ?? new Recorrido(sede.Sede.Ubicacion, null, [], 0);
        ultimaSalida = registro?.UltimaSalida;
    }

    public Recorrido Estado() { lock (turno) return actual; }

    public void ReanudarAutomatica()
    {
        lock (turno)
            if (actual.Horario.HasValue)
                while (!actual.Terminado) Avanzar();
    }

    public static Parada[] Planificar(Grafo grafo, string origen, Lista<Pedido> pendientes)
    {
        var restantes = new Lista<Pedido>();
        foreach (var pedido in pendientes) restantes.Agregar(pedido);
        var paradas = new Lista<Parada>();
        int total = 0;
        while (!restantes.EstaVacia())
        {
            var rutas = Dijkstra.Calcular(grafo, origen);
            Pedido? elegido = null;
            int minimo = int.MaxValue;
            foreach (var pedido in restantes)
            {
                if (!rutas.EsAlcanzable(pedido.Destino)) continue;
                int minutos = rutas.TiempoA(pedido.Destino);
                if (minutos < minimo) { minimo = minutos; elegido = pedido; }
            }
            if (elegido == null) break;
            string destino = elegido.Destino;
            var ids = new Lista<string>();
            foreach (var pedido in restantes)
                if (pedido.Destino == destino) ids.Agregar(pedido.Id);
            foreach (string id in ids) restantes.Eliminar(p => p.Id == id);
            total = checked(total + minimo);
            paradas.Agregar(new(destino, rutas.CaminoA(destino).ToArray(), minimo, total, ids.ToArray()));
            origen = destino;
        }
        return paradas.ToArray();
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
        var paradas = Planificar(pedidos.Grafo, actual.Origen, pedidos.ObtenerPendientes());
        Guardar(new(actual.Origen, horario, paradas, 0), horario ?? ultimaSalida);
    }

    private void Avanzar()
    {
        if (actual.Terminado) return;
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
