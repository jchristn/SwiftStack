namespace SwiftStack.RabbitMq
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using SerializationHelper;
    using SyslogLogging;
    using SwiftStack.Serialization;

    /// <summary>
    /// RabbitMQ broadcast receiver client.
    /// </summary>
    /// <typeparam name="T">Type of message to receive.  Must be JSON serializable.</typeparam>
    public class RabbitMqBroadcastReceiver<T> : IMessageQueueBroadcastReceiver<T> where T : class
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        /// <summary>
        /// Boolean indicating whether or not initialization has been performed.
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        /// <summary>
        /// Boolean indicating whether or not the channel is connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (_Connection != null) return _Connection.IsOpen;
                return false;
            }
        }

        /// <summary>
        /// Event to fire when a message is received.
        /// </summary>
        public event EventHandler<IncomingMessage<T>> MessageReceived;

        #endregion

        #region Private-Members

        private string _Header = "[RabbitMqBroadcastReceiver] ";
        private bool _IsInitialized = false;

        private ISerializer _Serializer = null;
        private LoggingModule _Logging = null;
        private QueueProperties _Queue = null;

        private ConnectionFactory _ConnectionFactory = null;
        private IConnection _Connection = null;
        private IChannel _ConsumerChannel = null;
        private AsyncEventingBasicConsumer _Consumer = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Create an instance.  Initialize the instance using InitializeAsync.
        /// </summary>
        /// <param name="serializer">Serializer.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="queue">Queue properties.</param>
        public RabbitMqBroadcastReceiver(
            ISerializer serializer,
            LoggingModule logging,
            QueueProperties queue)
        {
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Logging = logging ?? new LoggingModule();
            _Queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _Header = "[RabbitMqConsumer " + _Queue.FullyQualifiedName + "] ";
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_IsInitialized)
            {
                _Header = null;
                _Serializer = null;
                _Logging = null;
                _Queue = null;

                _ConnectionFactory = null;

                _ConsumerChannel?.CloseAsync().Wait();
                _ConsumerChannel?.Dispose();
                _Connection?.CloseAsync().Wait();
                _Connection?.Dispose();

                _Consumer = null;

                MessageReceived = null;
            }
        }

        /// <summary>
        /// Initialize the instance.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task InitializeAsync(CancellationToken token = default)
        {
            _ConnectionFactory = new ConnectionFactory { HostName = _Queue.Hostname };
            _Connection = await _ConnectionFactory.CreateConnectionAsync(cancellationToken: token).ConfigureAwait(false);
            _ConsumerChannel = await _Connection.CreateChannelAsync(cancellationToken: token).ConfigureAwait(false);

            await _ConsumerChannel.ExchangeDeclareAsync(
                exchange: _Queue.Name,
                type: ExchangeType.Fanout,
                durable: _Queue.Durable,
                autoDelete: _Queue.AutoDelete).ConfigureAwait(false);

            string queueName = (await _ConsumerChannel.QueueDeclareAsync(cancellationToken: token).ConfigureAwait(false)).QueueName;

            await _ConsumerChannel.QueueBindAsync(
                queue: queueName, 
                exchange: _Queue.Name, 
                routingKey: "",
                cancellationToken: token
                ).ConfigureAwait(false);

            _Consumer = new AsyncEventingBasicConsumer(_ConsumerChannel);

            _Consumer.ReceivedAsync += async (channel, ea) =>
            {
                byte[] body = ea.Body.ToArray();

                try
                {
                    string json = Encoding.UTF8.GetString(body);
                    T msg = _Serializer.DeserializeJson<T>(json);
                    MessageReceived?.Invoke(this, new IncomingMessage<T>
                    {
                        DeliveryTag = ea.DeliveryTag,
                        CorrelationId = ea.BasicProperties.CorrelationId,
                        Data = msg
                    });
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "exception while delivering message:" + Environment.NewLine + e.ToString());
                }
            };

            await _ConsumerChannel.BasicConsumeAsync(
                queue: queueName, 
                autoAck: true, 
                consumer: _Consumer,
                cancellationToken: token
                ).ConfigureAwait(false);

            _IsInitialized = true;
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
