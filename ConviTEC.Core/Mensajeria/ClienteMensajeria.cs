using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace ConviTEC.Core.Mensajeria
{
    /// <summary>
    /// Se conecta a RabbitMQ para que una sede pueda enviar y recibir mensajes.
    /// Cada sede tiene su propio buzón (cola): "convitec.sede.CODIGO".
    /// </summary>
    public class ClienteMensajeria : IAsyncDisposable
    {
        public const string EXCHANGE_DIFUSION = "convitec.difusion";

        private readonly ConnectionFactory fabrica;
        private IConnection? conexion;
        private IChannel? canalEnvio;       // canal para mandar
        private IChannel? canalRecepcion;   // canal para recibir

        public string CodigoSede { get; private set; }

        public bool Conectado
        {
            get { return conexion != null && conexion.IsOpen; }
        }

        public ClienteMensajeria(string codigoSede, string host = "localhost", int puerto = 5672,
                                 string usuario = "guest", string clave = "guest")
        {
            if (string.IsNullOrWhiteSpace(codigoSede))
                throw new ArgumentException("El código de la sede no puede estar vacío.");

            CodigoSede = codigoSede.Trim();
            fabrica = new ConnectionFactory
            {
                HostName = host,
                Port = puerto,
                UserName = usuario,
                Password = clave,
                ClientProvidedName = "ConviTEC-" + CodigoSede   // nombre que se ve en el panel
            };
        }

        // ---- Nombre del buzón de una sede ----
        public static string NombreCola(string codigoSede)
        {
            return "convitec.sede." + codigoSede;
        }

        // =========================================================
        //  CONECTAR
        // =========================================================
        public async Task ConectarAsync()
        {
            try
            {
                conexion = await fabrica.CreateConnectionAsync();
            }
            catch (BrokerUnreachableException ex)
            {
                throw new InvalidOperationException(
                    $"No se pudo conectar a RabbitMQ en {fabrica.HostName}:{fabrica.Port}. ¿Está encendido? ({ex.Message})");
            }

            canalEnvio = await conexion.CreateChannelAsync();
            canalRecepcion = await conexion.CreateChannelAsync();

            // El "repartidor" que copia los avisos a todos los buzones
            await canalEnvio.ExchangeDeclareAsync(EXCHANGE_DIFUSION, ExchangeType.Fanout, durable: true, autoDelete: false);

            // Mi propio buzón
            await DeclararColaAsync(canalEnvio, CodigoSede);
        }

        // Crea el buzón de una sede (si ya existe no pasa nada) y lo conecta al repartidor
        private static async Task DeclararColaAsync(IChannel canal, string codigoSede)
        {
            string cola = NombreCola(codigoSede);
            await canal.QueueDeclareAsync(cola, durable: true, exclusive: false, autoDelete: false, arguments: null);
            await canal.QueueBindAsync(cola, EXCHANGE_DIFUSION, routingKey: "");
        }

        // =========================================================
        //  ENVIAR
        // =========================================================

        // ---- Enviar a UNA sede ----
        public async Task EnviarAsync(Mensaje mensaje)
        {
            VerificarConexion();
            if (mensaje == null)
                throw new ArgumentException("El mensaje no puede ser nulo.");
            if (string.IsNullOrWhiteSpace(mensaje.SedeDestino))
                throw new ArgumentException("El mensaje no tiene sede destino. Para enviar a todas use DifundirAsync.");

            if (string.IsNullOrWhiteSpace(mensaje.SedeOrigen))
                mensaje.SedeOrigen = CodigoSede;

            // Se asegura de que el buzón destino exista, así la carta espera aunque esa sede esté apagada
            await DeclararColaAsync(canalEnvio!, mensaje.SedeDestino);
            await PublicarAsync("", NombreCola(mensaje.SedeDestino), mensaje);
        }

        // ---- Enviar a TODAS las sedes ----
        public async Task DifundirAsync(Mensaje mensaje)
        {
            VerificarConexion();
            if (mensaje == null)
                throw new ArgumentException("El mensaje no puede ser nulo.");

            mensaje.SedeDestino = "";
            if (string.IsNullOrWhiteSpace(mensaje.SedeOrigen))
                mensaje.SedeOrigen = CodigoSede;

            await PublicarAsync(EXCHANGE_DIFUSION, "", mensaje);
        }

        private async Task PublicarAsync(string exchange, string routingKey, Mensaje mensaje)
        {
            byte[] cuerpo = Encoding.UTF8.GetBytes(mensaje.ToJson());
            BasicProperties propiedades = new BasicProperties
            {
                Persistent = true,                 // no se pierde si RabbitMQ se reinicia
                ContentType = "application/json",
                MessageId = mensaje.Id
            };

            try
            {
                await canalEnvio!.BasicPublishAsync(exchange, routingKey, mandatory: false,
                                                    basicProperties: propiedades, body: cuerpo);
            }
            catch (Exception ex) when (ex is AlreadyClosedException || ex is OperationInterruptedException)
            {
                throw new InvalidOperationException("Se perdió la conexión con RabbitMQ al enviar: " + ex.Message);
            }
        }

        // =========================================================
        //  RECIBIR
        // =========================================================

        // alRecibir = lo que la sede hace con cada carta que le llega
        public async Task EscucharAsync(Func<Mensaje, Task> alRecibir)
        {
            VerificarConexion();
            if (alRecibir == null)
                throw new ArgumentException("Debe indicar qué hacer con los mensajes recibidos.");

            IChannel canal = canalRecepcion!;
            await canal.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);   // de una en una

            AsyncEventingBasicConsumer consumidor = new AsyncEventingBasicConsumer(canal);
            consumidor.ReceivedAsync += async (emisor, entrega) =>
            {
                try
                {
                    string json = Encoding.UTF8.GetString(entrega.Body.ToArray());
                    Mensaje mensaje = Mensaje.DesdeJson(json);

                    if (mensaje.SedeOrigen != CodigoSede)   // ignora sus propias difusiones
                        await alRecibir(mensaje);
                }
                catch (Exception ex)
                {
                    // Un mensaje malo no debe tumbar la sede
                    Console.WriteLine($"[{CodigoSede}] Error al procesar un mensaje: {ex.Message}");
                }

                // "Recibido": RabbitMQ ya puede borrar la carta del buzón
                await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false);
            };

            await canal.BasicConsumeAsync(NombreCola(CodigoSede), autoAck: false, consumer: consumidor);
        }

        // =========================================================
        //  OTROS
        // =========================================================

        // Borra el buzón de esta sede (solo se usa en las pruebas para limpiar)
        public async Task EliminarMiColaAsync()
        {
            VerificarConexion();
            await canalEnvio!.QueueDeleteAsync(NombreCola(CodigoSede), ifUnused: false, ifEmpty: false);
        }

        private void VerificarConexion()
        {
            if (!Conectado || canalEnvio == null || canalRecepcion == null)
                throw new InvalidOperationException("No hay conexión con RabbitMQ. Llame primero a ConectarAsync().");
        }

        // ---- Cerrar todo al terminar ----
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (canalRecepcion != null) await canalRecepcion.DisposeAsync();
                if (canalEnvio != null) await canalEnvio.DisposeAsync();
                if (conexion != null) await conexion.DisposeAsync();
            }
            catch (Exception)
            {
                // si ya estaba cerrado, no importa
            }
        }
    }
}