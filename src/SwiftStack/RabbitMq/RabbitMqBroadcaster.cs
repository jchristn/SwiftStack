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
    /// RabbitMQ broadcaster client.
    /// </summary>
    /// <typeparam name="T">Type of message to broadcast.  Must be JSON serializable.</typeparam>
    public class RabbitMqBroadcaster<T> : IMessageQueueBroadcaster<T>, IDisposable where T : class
    {
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
        /// Debug logging.
        /// </summary>
        public bool Debug { get; set; } = false;

        #endregion

        #region Private-Members

        private string _Header = "[RabbitMqBroadcaster] ";
        private bool _IsInitialized = false;
        private int _MaxMessageSize = 32 * 1024 * 1024;

        private ISerializer _Serializer = null;
        private LoggingModule _Logging = null;
        private QueueProperties _Queue = null;

        private ConnectionFactory _ConnectionFactory = null;
        private IConnection _Connection = null;
        private IChannel _Channel = null;

        private SemaphoreSlim _Semaphore = new SemaphoreSlim(1, 1);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Create an instance.  Initialize the instance using InitializeAsync.
        /// </summary>
        /// <param name="serializer">Serializer.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="queue">Queue properties.</param>
        /// <param name="maxMessageSize">Maximum message size.</param>
        public RabbitMqBroadcaster(
            ISerializer serializer,
            LoggingModule logging,
            QueueProperties queue,
            int maxMessageSize = (32 * 1024 * 1024))
        {
            if (maxMessageSize < 1024) throw new ArgumentOutOfRangeException(nameof(maxMessageSize));

            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Logging = logging ?? new LoggingModule();
            _Queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _MaxMessageSize = maxMessageSize;
            _Header = "[RabbitMqBroadcaster " + _Queue.FullyQualifiedName + "] ";
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

                _Channel?.CloseAsync().Wait();
                _Channel?.Dispose();
                _Connection?.CloseAsync().Wait();
                _Connection?.Dispose();

                _IsInitialized = false;
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
            _Channel = await _Connection.CreateChannelAsync(cancellationToken: token).ConfigureAwait(false);
            await _Channel.ExchangeDeclareAsync(
                exchange: _Queue.Name,
                type: ExchangeType.Fanout,
                durable: _Queue.Durable,
                autoDelete: _Queue.AutoDelete,
                cancellationToken: token).ConfigureAwait(false);

            _IsInitialized = true;
        }

        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <param name="correlationId">Correlation ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Broadcast(
            T msg,
            string correlationId,
            CancellationToken token = default)
        {
            ValidateInitialization();
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            if (String.IsNullOrEmpty(correlationId)) correlationId = Guid.NewGuid().ToString();

            BasicProperties props = new BasicProperties
            {
                CorrelationId = correlationId
            };

            string json = _Serializer.SerializeJson(msg, false);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            if (bytes.Length > _MaxMessageSize)
            {
                _Logging.Alert(_Header + "message exceeds maximum size of " + _MaxMessageSize + ": " + bytes.Length);
                return;
            }

            if (Debug) _Logging.Debug(_Header + "sending message:" + Environment.NewLine + json);
            else _Logging.Debug(_Header + "sending message: " + bytes.Length + " bytes");

            await _Semaphore.WaitAsync(token).ConfigureAwait(false);

            try
            {
                await _Channel.BasicPublishAsync(
                    exchange: _Queue.Name,
                    routingKey: "",
                    mandatory: false,
                    basicProperties: props,
                    body: bytes,
                    cancellationToken: token
                    ).ConfigureAwait(false);
            }
            finally
            {
                _Semaphore.Release();
            }
        }

        #endregion

        #region Private-Methods

        private void ValidateInitialization()
        {
            if (!_IsInitialized) throw new InvalidOperationException("Initialize using InitializeAsync before using instance methods.");
        }

        #endregion
    }
}
