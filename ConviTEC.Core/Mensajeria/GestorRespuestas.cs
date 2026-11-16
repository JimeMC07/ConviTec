using ConviTEC.Core.Estructuras;

namespace ConviTEC.Core.Mensajeria
{
    /// <summary>
    /// Guarda las solicitudes que están esperando respuesta
    /// y entrega cada respuesta a quien la estaba esperando.
    /// </summary>
    public class GestorRespuestas
    {
        private class Espera
        {
            public string IdSolicitud { get; set; } = "";
            public TaskCompletionSource<Mensaje> Promesa { get; set; } = null!;
        }

        private readonly Lista<Espera> esperas = new Lista<Espera>();
        private readonly object candado = new object();

        // ---- Registrar que voy a esperar la respuesta a esta solicitud ----
        public Task<Mensaje> Registrar(string idSolicitud)
        {
            TaskCompletionSource<Mensaje> promesa =
                new TaskCompletionSource<Mensaje>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (candado)
                esperas.Agregar(new Espera { IdSolicitud = idSolicitud, Promesa = promesa });

            return promesa.Task;
        }

        // ---- Llegó una respuesta: se la entrego a quien esperaba ----
        // Devuelve false si nadie la esperaba (por ejemplo, llegó tarde).
        public bool Resolver(Mensaje respuesta)
        {
            Espera? espera;
            lock (candado)
            {
                espera = esperas.Buscar(e => e.IdSolicitud == respuesta.IdRespuestaA);
                if (espera != null)
                    esperas.Eliminar(e => e == espera);
            }

            if (espera == null) return false;
            espera.Promesa.TrySetResult(respuesta);
            return true;
        }

        // ---- Ya no espero esta respuesta (se acabó el tiempo) ----
        public void Cancelar(string idSolicitud)
        {
            lock (candado)
                esperas.Eliminar(e => e.IdSolicitud == idSolicitud);
        }
    }
}