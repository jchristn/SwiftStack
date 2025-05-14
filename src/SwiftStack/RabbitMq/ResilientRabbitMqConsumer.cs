namespace SwiftStack.RabbitMq
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using PersistentCollection;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Client.Exceptions;
    using SyslogLogging;
    using SerializationHelper;

    /// <summary>
    /// Resilient RabbitMQ consumer client.
    /// </summary>
    /// <typeparam name="T">Type of message to consume.  Must be JSON serializable.</typeparam>
    public class ResilientRabbitMqConsumer<T> : IMessageQueueConsumer<T> where T : class
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

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

        private string _Header = "[ResilientRabbitMqConsumer] ";

        private Serializer _Serializer = new Serializer();
        private LoggingModule _Logging = null;
        private QueueProperties _Queue = null;
        private int _MaxParallelTasks = 4;
        private bool _AutoAcknowledge = false;

        private string _StoreAndForwardPath = null;
        private PersistentQueue<StoreAndForwardMessage> _StoreAndForwardQueue = null;

        private ConnectionFactory _ConnectionFactory = null;
        private IConnection _Connection = null;
        private IChannel _Channel = null;
        private AsyncEventingBasicConsumer _Consumer = null;

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private Task _ConnectionTask = null;
        private Task _DeliveryTask = null;
        private Task _AcknowledgementTask = null;

        private readonly object _AcknowledgementLock = new object();
        private List<ulong> _AcknowledgeMessages = new List<ulong>();
        private List<ulong> _RejectWithoutRequeueMessages = new List<ulong>();
        private List<ulong> _RejectWithRequeueMessages = new List<ulong>();

        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Create an instance.
        /// </summary>
        /// <param name="logging">Logging module.</param>
        /// <param name="queue">Queue properties.</param>
        /// <param name="storeAndForwardPath">Directory for store-and-forward data.</param>
        /// <param name="maxParallelTasks">Maximum number of parallel tasks.</param>
        /// <param name="autoAck">Automatically acknowledge messages; default is true.</param>
        public ResilientRabbitMqConsumer(
            LoggingModule logging, 
            QueueProperties queue,
            string storeAndForwardPath,
            int maxParallelTasks = 4,
            bool autoAck = true)
        {
            if (String.IsNullOrEmpty(storeAndForwardPath)) throw new ArgumentNullException(nameof(storeAndForwardPath));
            if (maxParallelTasks < 1) throw new ArgumentOutOfRangeException(nameof(maxParallelTasks));

            _Logging = logging ?? new LoggingModule();
            _Queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _StoreAndForwardPath = storeAndForwardPath;
            _MaxParallelTasks = maxParallelTasks;
            _AutoAcknowledge = autoAck;
            _Header = "[ResilientRabbitMqConsumer " + _Queue.FullyQualifiedName + "] ";

            _StoreAndForwardQueue = new PersistentQueue<StoreAndForwardMessage>(_StoreAndForwardPath);

            _ConnectionTask = Task.Run(() => ConnectionTask(), _TokenSource.Token);
            _DeliveryTask = Task.Run(() => DeliveryTask(), _TokenSource.Token);
            _AcknowledgementTask = Task.Run(() => AcknowledgementTask(), _TokenSource.Token);

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
            if (disposing && !_Disposed)
            {
                try
                {
                    // Signal cancellation to all tasks
                    _TokenSource?.Cancel();

                    // Wait for tasks to complete with a reasonable timeout
                    Task.WhenAll(
                        _ConnectionTask ?? Task.CompletedTask,
                        _AcknowledgementTask ?? Task.CompletedTask
                    ).Wait(TimeSpan.FromSeconds(5));

                    // Clean up resources
                    _Header = null;
                    _Serializer = null;
                    _Logging = null;
                    _Queue = null;

                    _ConnectionFactory = null;

                    // Close and dispose channel
                    if (_Channel != null)
                    {
                        try { _Channel.CloseAsync().Wait(TimeSpan.FromSeconds(2)); } catch { }
                        try { _Channel.Dispose(); } catch { }
                        _Channel = null;
                    }

                    // Close and dispose connection
                    if (_Connection != null)
                    {
                        try { _Connection.CloseAsync().Wait(TimeSpan.FromSeconds(2)); } catch { }
                        try { _Connection.Dispose(); } catch { }
                        _Connection = null;
                    }

                    _Consumer = null;
                    MessageReceived = null;

                    // Clean up the token source
                    _TokenSource?.Dispose();
                    _TokenSource = null;

                    _ConnectionTask = null;
                    _AcknowledgementTask = null;

                    // Clear acknowledgement lists
                    lock (_AcknowledgementLock)
                    {
                        _AcknowledgeMessages?.Clear();
                        _RejectWithoutRequeueMessages?.Clear();
                        _RejectWithRequeueMessages?.Clear();
                    }

                    _Disposed = true;
                }
                catch (Exception ex)
                {
                    // Log exception during disposal
                    _Logging?.Error(_Header + "error during disposal: " + ex.Message);
                }
            }
        }

        /// <inheritdoc />
        public async Task Acknowledge(ulong deliveryTag, CancellationToken token = default)
        {
            lock (_AcknowledgementLock)
            {
                if (!_AcknowledgeMessages.Contains(deliveryTag)) _AcknowledgeMessages.Add(deliveryTag);
            }
        }

        /// <inheritdoc />
        public async Task Reject(ulong deliveryTag, bool requeue = true, CancellationToken token = default)
        {
            lock (_AcknowledgementLock)
            {
                if (requeue)
                {
                    if (!_RejectWithRequeueMessages.Contains(deliveryTag)) _RejectWithRequeueMessages.Add(deliveryTag);
                }
                else
                {
                    if (!_RejectWithoutRequeueMessages.Contains(deliveryTag)) _RejectWithoutRequeueMessages.Add(deliveryTag);
                }
            }
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

                    if (_Consumer != null) _Consumer = null;

                    _Logging.Info(_Header + "attempting connection to host " + _Queue.Hostname);

                    _Connection = await _ConnectionFactory.CreateConnectionAsync(cancellationToken: _TokenSource.Token).ConfigureAwait(false);
                    _Channel = await _Connection.CreateChannelAsync(cancellationToken: _TokenSource.Token).ConfigureAwait(false);

                    await _Channel.QueueDeclareAsync(
                        queue: _Queue.Name,
                        durable: _Queue.Durable,
                        exclusive: _Queue.Exclusive,
                        autoDelete: _Queue.AutoDelete
                        ).ConfigureAwait(false);

                    _Consumer = new AsyncEventingBasicConsumer(_Channel);

                    _Consumer.ReceivedAsync += async (channel, ea) =>
                    {
                        _Logging.Debug(
                            _Header + 
                            "queuing message with correlation ID " + 
                            (!String.IsNullOrEmpty(ea.BasicProperties.CorrelationId) ? ea.BasicProperties.CorrelationId : "(null)"));


                        _StoreAndForwardQueue.Enqueue(new StoreAndForwardMessage
                        {
                            DeliveryTag = ea.DeliveryTag,
                            CorrelationId = ea.BasicProperties.CorrelationId,
                            Data = ea.Body.ToArray()
                        });
                    };

                    await _Channel.BasicConsumeAsync(
                        queue: _Queue.Name,
                        autoAck: _AutoAcknowledge,
                        consumer: _Consumer
                        ).ConfigureAwait(false);

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

        private async Task DeliveryTask()
        {
            int currentTasks = 0;

            byte[] body = null;

            while (!_TokenSource.IsCancellationRequested)
            {
                try
                {
                    if (currentTasks >= _MaxParallelTasks || _StoreAndForwardQueue.Count < 1)
                    {
                        await Task.Delay(500, _TokenSource.Token).ConfigureAwait(false);
                        continue;
                    }

                    StoreAndForwardMessage sfm = _StoreAndForwardQueue.Dequeue();
                    body = sfm.Data;

                    _Logging.Debug(
                        _Header +
                        "dequeued message with correlation ID " +
                        (!String.IsNullOrEmpty(sfm.CorrelationId) ? sfm.CorrelationId : "(null)"));

                    try
                    {
                        string json = Encoding.UTF8.GetString(body);
                        T msg = _Serializer.DeserializeJson<T>(json);

                        currentTasks++;
                        MessageReceived?.Invoke(this, new IncomingMessage<T>
                        {
                            DeliveryTag = sfm.DeliveryTag,
                            CorrelationId = sfm.CorrelationId,
                            Data = msg
                        });
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn(
                            _Header + 
                            "exception while delivering message:" + Environment.NewLine + 
                            e.ToString() + 
                            (body != null ? Environment.NewLine + "Body:" + Environment.NewLine + Encoding.UTF8.GetString(body) : null));
                    }
                    finally
                    {
                        currentTasks--;
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
                catch (Exception e)
                {
                    _Logging.Warn(
                        _Header +
                        "exception in delivery task:" + Environment.NewLine +
                        e.ToString() +
                        (body != null ? Environment.NewLine + "Body:" + Environment.NewLine + Encoding.UTF8.GetString(body) : null));

                    await Task.Delay(2000, _TokenSource.Token).ConfigureAwait(false);
                }
            }
        }

        private async Task AcknowledgementTask()
        {
            while (!_TokenSource.IsCancellationRequested)
            {
                try
                {
                    if (_Connection == null || !_Connection.IsOpen || !_Channel.IsOpen)
                    {
                        await Task.Delay(1000, _TokenSource.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        List<ulong> acknowledgeTags = new List<ulong>();
                        List<ulong> rejectWithoutRequeueTags = new List<ulong>();
                        List<ulong> rejectWithRequeueTags = new List<ulong>();

                        lock (_AcknowledgementLock)
                        {
                            if (_AcknowledgeMessages.Count > 0)
                                acknowledgeTags = new List<ulong>(_AcknowledgeMessages);

                            if (_RejectWithoutRequeueMessages.Count > 0)
                                rejectWithoutRequeueTags = new List<ulong>(_RejectWithoutRequeueMessages);

                            if (_RejectWithRequeueMessages.Count > 0)
                                rejectWithRequeueTags = new List<ulong>(_RejectWithRequeueMessages);
                        }

                        if (acknowledgeTags.Count > 0)
                        {
                            foreach (ulong tag in acknowledgeTags)
                            {
                                try
                                {
                                    await _Channel.BasicAckAsync(
                                        deliveryTag: tag,
                                        multiple: false,
                                        cancellationToken: _TokenSource.Token
                                        ).ConfigureAwait(false);

                                    lock (_AcknowledgementLock) _AcknowledgeMessages.Remove(tag);
                                }
                                catch (Exception e)
                                {
                                    _Logging.Warn(_Header + "failed to send acknowledgement for delivery tag " + tag + ": " + Environment.NewLine + e.ToString());
                                }
                            }
                        }

                        if (rejectWithoutRequeueTags.Count > 0)
                        {
                            foreach (ulong tag in rejectWithoutRequeueTags)
                            {
                                try
                                {
                                    await _Channel.BasicNackAsync(
                                        deliveryTag: tag,
                                        multiple: false,
                                        requeue: false,
                                        cancellationToken: _TokenSource.Token
                                        ).ConfigureAwait(false);

                                    lock (_AcknowledgementLock) _RejectWithoutRequeueMessages.Remove(tag);
                                }
                                catch (Exception e)
                                {
                                    _Logging.Warn(_Header + "failed to send reject without requeue for delivery tag " + tag + ": " + Environment.NewLine + e.ToString());
                                }
                            }
                        }

                        if (rejectWithRequeueTags.Count > 0)
                        {
                            foreach (ulong tag in rejectWithRequeueTags)
                            {
                                try
                                {
                                    await _Channel.BasicNackAsync(
                                        deliveryTag: tag,
                                        multiple: false,
                                        requeue: true,
                                        cancellationToken: _TokenSource.Token
                                        ).ConfigureAwait(false);

                                    lock (_AcknowledgementLock) _RejectWithRequeueMessages.Remove(tag);
                                }
                                catch (Exception e)
                                {
                                    _Logging.Warn(_Header + "failed to send reject with requeue for delivery tag " + tag + ": " + Environment.NewLine + e.ToString());
                                }
                            }
                        }
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
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "acknowledgement task exception:" + Environment.NewLine + e.ToString());
                    continue;
                }
            }
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
