namespace SwiftStack.RabbitMq
{
    using System;
    using System.Diagnostics;
    using System.Security.Authentication.ExtendedProtection;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using SyslogLogging;
    using SerializationHelper;

    /// <summary>
    /// RabbitMQ producer client.
    /// </summary>
    /// <typeparam name="T">Type of message to produce.  Must be JSON serializable.</typeparam>
    public class RabbitMqProducer<T> : IDisposable, IMessageQueueProducer<T> where T : class
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
        /// Debug logging.
        /// </summary>
        public bool Debug { get; set; } = false;

        #endregion

        #region Private-Members

        private string _Header = "[RabbitMqProducer] ";
        private bool _IsInitialized = false;
        private int _MaxMessageSize = 32 * 1024 * 1024;

        private Serializer _Serializer = new Serializer();
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
        /// <param name="logging">Logging module.</param>
        /// <param name="queue">Queue properties.</param>
        /// <param name="maxMessageSize">Maximum message size.</param>
        public RabbitMqProducer(
            LoggingModule logging, 
            QueueProperties queue, 
            int maxMessageSize = (32 * 1024 * 1024))
        {
            if (maxMessageSize < 1024) throw new ArgumentOutOfRangeException(nameof(maxMessageSize));

            _Logging = logging ?? new LoggingModule();
            _Queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _MaxMessageSize = maxMessageSize;
            _Header = "[RabbitMqProducer " + _Queue.FullyQualifiedName + "] ";

            _Logging.Debug(_Header + "initialized");
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
            await _Channel.QueueDeclareAsync(
                queue: _Queue.Name, 
                durable: _Queue.Durable, 
                exclusive: _Queue.Exclusive, 
                autoDelete: _Queue.AutoDelete
                ).ConfigureAwait(false);

            _IsInitialized = true;
        }

        /// <inheritdoc />
        public async Task SendMessage(
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
                    exchange: "",
                    routingKey: _Queue.Name,
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

        /// <inheritdoc />
        public async Task SendMessage(
            T msg,
            bool persist,
            string correlationId,
            CancellationToken token = default)
        {
            ValidateInitialization();
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            if (String.IsNullOrEmpty(correlationId)) correlationId = Guid.NewGuid().ToString();

            BasicProperties props = new BasicProperties
            {
                Persistent = persist,
                CorrelationId = correlationId
            };

            byte[] bytes = Encoding.UTF8.GetBytes(_Serializer.SerializeJson(msg));

            if (bytes.Length > _MaxMessageSize)
            {
                _Logging.Alert(_Header + "message exceeds maximum size of " + _MaxMessageSize + ": " + bytes.Length);
                return;
            }

            await _Channel.BasicPublishAsync(
                exchange: "",
                routingKey: _Queue.Name,
                mandatory: false,
                basicProperties: props,
                body: bytes,
                cancellationToken: token
                ).ConfigureAwait(false);
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
