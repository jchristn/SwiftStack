namespace SwiftStack.RabbitMq
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using SwiftStack.Serialization;
    using SyslogLogging;

    /// <summary>
    /// RabbitMQ consumer client.
    /// </summary>
    /// <typeparam name="T">Type of message to consume.  Must be JSON serializable.</typeparam>
    public class RabbitMqConsumer<T> : IMessageQueueConsumer<T> where T : class
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

        private string _Header = "[RabbitMqConsumer] ";
        private bool _IsInitialized = false;

        private ISerializer _Serializer = null;
        private LoggingModule _Logging = null;
        private QueueProperties _Queue = null;
        private bool _AutoAcknowledge = false;

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
        /// <param name="autoAck">Automatically acknowledge messages; default is true.</param>
        public RabbitMqConsumer(ISerializer serializer, LoggingModule logging, QueueProperties queue, bool autoAck = true)
        {
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Logging = logging ?? new LoggingModule();
            _Queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _AutoAcknowledge = autoAck;
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

            await _ConsumerChannel.QueueDeclareAsync(
                queue: _Queue.Name, 
                durable: _Queue.Durable, 
                exclusive: _Queue.Exclusive, 
                autoDelete: _Queue.AutoDelete,
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
                queue: _Queue.Name, 
                autoAck: _AutoAcknowledge, 
                consumer: _Consumer
                ).ConfigureAwait(false);

            _IsInitialized = true;
        }

        /// <inheritdoc />
        public async Task Acknowledge(ulong deliveryTag, CancellationToken token = default)
        {
            ValidateInitialization();

            try
            {
                await _ConsumerChannel.BasicAckAsync(
                    deliveryTag: deliveryTag,
                    multiple: false,
                    cancellationToken: token
                    ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "acknowledge exception:" + Environment.NewLine + e.ToString());
            }
        }

        /// <inheritdoc />
        public async Task Reject(ulong deliveryTag, bool requeue = true, CancellationToken token = default)
        {
            ValidateInitialization();

            try
            {
                await _ConsumerChannel.BasicNackAsync(
                    deliveryTag: deliveryTag,
                    multiple: false,
                    requeue: requeue,
                    cancellationToken: token
                    ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "reject exception:" + Environment.NewLine + e.ToString());
            }
        }

        #endregion

        #region Private-Methods

        private void ValidateInitialization()
        {
            if (!_IsInitialized) throw new InvalidOperationException("Initialize using InitializeAsync before using instance methods.");
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
