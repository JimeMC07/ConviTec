using ConviTEC.Core.Logica;

class HorarioEntregas(ServicioEntregas entregas) : BackgroundService
{
    private static readonly TimeZoneInfo CostaRica = TimeZoneInfo.FindSystemTimeZoneById("America/Costa_Rica");

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        // Completar una salida automática que ya tenía su lote guardado al cerrar.
        entregas.ReanudarAutomatica();
        while (!cancelacion.IsCancellationRequested)
        {
            DateTime ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CostaRica);
            DateTime salida = ahora.Date.AddHours(11);
            if (ahora > salida) salida = ahora.Date.AddHours(15);
            if (ahora > salida) salida = ahora.Date.AddDays(1).AddHours(11);
            await Task.Delay(salida - ahora, cancelacion);
            entregas.Despachar(salida);
        }
    }
}
