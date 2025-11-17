namespace SwiftStack.RabbitMq
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using PersistentCollection;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Client.Exceptions;
    using SyslogLogging;
    using SwiftStack.Serialization;

    /// <summary>
    /// Resilient RabbitMQ broadcaster client.
    /// </summary>
    /// <typeparam name="T">Type of message to broadcast.  Must be JSON serializable.</typeparam>
    public class ResilientRabbitMqBroadcaster<T> : IMessageQueueBroadcaster<T>, IDisposable where T : class
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        /// <summary>
        /// Debug logging.
        /// </summary>
        public bool Debug { get; set; } = false;

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

        #endregion

        #region Private-Members

        private string _Header = "[ResilientRabbitMqBroadcaster] ";
        private int _MaxMessageSize = 32 * 1024 * 1024;

        private ISerializer _Serializer = null;
        private LoggingModule _Logging = null;
        private QueueProperties _Queue = null;

        private string _StoreAndForwardPath = null;
        private PersistentQueue<StoreAndForwardMessage> _StoreAndForwardQueue = null;

        private ConnectionFactory _ConnectionFactory = null;
        private IConnection _Connection = null;
        private IChannel _Channel = null;

        private SemaphoreSlim _Semaphore = new SemaphoreSlim(1, 1);

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private Task _ConnectionTask = null;
        private Task _StoreAndForwardTask = null;

        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Create an instance.
        /// </summary>
        /// <param name="serializer">Serializer.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="queue">Queue properties.</param>
        /// <param name="storeAndForwardPath">Directory for store-and-forward data.</param>
        /// <param name="maxMessageSize">Maximum message size.</param>
        public ResilientRabbitMqBroadcaster(
            ISerializer serializer,
            LoggingModule logging,
            QueueProperties queue,
            string storeAndForwardPath,
            int maxMessageSize = (32 * 1024 * 1024))
        {
            if (String.IsNullOrEmpty(storeAndForwardPath)) throw new ArgumentNullException(nameof(storeAndForwardPath));
            if (maxMessageSize < 1024) throw new ArgumentOutOfRangeException(nameof(maxMessageSize));

            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Logging = logging ?? new LoggingModule();
            _Queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _MaxMessageSize = maxMessageSize;
            _Header = "[ResilientRabbitMqProducer " + _Queue.FullyQualifiedName + "] ";

            _ConnectionTask = Task.Run(() => ConnectionTask(), _TokenSource.Token);

            _StoreAndForwardPath = storeAndForwardPath;
            _StoreAndForwardQueue = new PersistentQueue<StoreAndForwardMessage>(_StoreAndForwardPath);
            _StoreAndForwardTask = Task.Run(() => StoreAndForwardTask(), _TokenSource.Token);

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
            if (!_Disposed)
            {
                _TokenSource?.Cancel();
                Task.WhenAll(
                    _ConnectionTask ?? Task.CompletedTask,
                    _StoreAndForwardTask ?? Task.CompletedTask
                ).Wait(TimeSpan.FromSeconds(5));

                _Header = null;
                _Serializer = null;
                _Logging = null;
                _Queue = null;

                _StoreAndForwardQueue?.Dispose();
                _StoreAndForwardQueue = null;
                _StoreAndForwardPath = null;
                _StoreAndForwardTask = null;

                _ConnectionTask = null;

                _ConnectionFactory = null;

                _Channel?.CloseAsync().Wait();
                _Channel?.Dispose();
                _Connection?.CloseAsync().Wait();
                _Connection?.Dispose();

                _Disposed = true;
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
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            if (String.IsNullOrEmpty(correlationId)) correlationId = Guid.NewGuid().ToString();

            byte[] bytes = Encoding.UTF8.GetBytes(_Serializer.SerializeJson(msg));

            if (bytes.Length > _MaxMessageSize)
            {
                _Logging.Alert(_Header + "message exceeds maximum size of " + _MaxMessageSize + ": " + bytes.Length);
                return;
            }

            _StoreAndForwardQueue.Enqueue(new StoreAndForwardMessage
            {
                CorrelationId = correlationId,
                Data = bytes
            });
        }

        #endregion

        #region Private-Methods

        private async Task ConnectionTask()
        {
            _ConnectionFactory = new ConnectionFactory { HostName = _Queue.Hostname };

            while (!_TokenSource.IsCancellationRequested)
            {
                try
                {
                    if (_Connection != null && _Connection.IsOpen && _Channel != null && _Channel.IsOpen)
                    {
                        await Task.Delay(1000, _TokenSource.Token).ConfigureAwait(false);
                        continue;
                    }
                    else
                    {
                        _Logging.Info(_Header + "reconnecting to host " + _Queue.Hostname);
                    }

                    if (_Channel != null)
                    {
                        try { _Channel.Dispose(); } catch { }
                        _Channel = null;
                    }

                    if (_Connection != null)
                    {
                        try { _Connection.Dispose(); } catch { }
                        _Connection = null;
                    }

                    _Connection = await _ConnectionFactory.CreateConnectionAsync(cancellationToken: _TokenSource.Token).ConfigureAwait(false);
                    _Channel = await _Connection.CreateChannelAsync(cancellationToken: _TokenSource.Token).ConfigureAwait(false);

                    await _Channel.ExchangeDeclareAsync(
                        exchange: _Queue.Name,
                        type: ExchangeType.Fanout,
                        durable: _Queue.Durable,
                        autoDelete: _Queue.AutoDelete,
                        cancellationToken: _TokenSource.Token).ConfigureAwait(false);

                    if (_Connection != null && _Connection.IsOpen && _Channel != null && _Channel.IsOpen)
                    {
                        _Logging.Info(_Header + "connected successfully to host " + _Queue.Hostname);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (BrokerUnreachableException bue)
                {
                    _Logging.Warn(_Header + "connection failed to host " + _Queue.Hostname + ": " + bue.Message);

                    await Task.Delay(2000, _TokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "exception in connection task: " + Environment.NewLine + e.ToString());

                    await Task.Delay(2000, _TokenSource.Token).ConfigureAwait(false);
                }
            }
        }

        private async Task StoreAndForwardTask()
        {
            while (!_TokenSource.IsCancellationRequested)
            {
                try
                {
                    if (_Connection == null || !_Connection.IsOpen)
                    {
                        await Task.Delay(1000, _TokenSource.Token).ConfigureAwait(false);
                        continue;
                    }

                    if (_StoreAndForwardQueue == null) break;

                    if (_StoreAndForwardQueue.Count < 1)
                    {
                        await Task.Delay(1000, _TokenSource.Token).ConfigureAwait(false);
                        continue;
                    }

                    StoreAndForwardMessage msg = _StoreAndForwardQueue.Dequeue();
                    if (msg == null || msg.Data == null || msg.Data.Length < 1) continue;

                    BasicProperties props = new BasicProperties
                    {
                        Persistent = msg.Persistent,
                        CorrelationId = msg.CorrelationId
                    };

                    await _Channel.BasicPublishAsync(
                        exchange: _Queue.Name,
                        routingKey: "",
                        mandatory: false,
                        basicProperties: props,
                        body: msg.Data,
                        cancellationToken: _TokenSource.Token
                        ).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (InvalidOperationException ioe)
                {
                    _Logging.Warn(
                        _Header +
                        "invalid operation detected likely due to queue race condition, continuing" +
                        Environment.NewLine +
                        ioe.ToString());
                    continue;
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "exception while sending message:" + Environment.NewLine + e.ToString());
                    continue;
                }
            }
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
