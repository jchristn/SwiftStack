namespace SwiftStack.RabbitMq
{
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;
    using WatsonWebserver.Core;
    using WatsonWebserver;

    /// <summary>
    /// SwiftStack RabbitMQ application.
    /// </summary>
    public class RabbitMqApp : IDisposable
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        /// <summary>
        /// Boolean to indicate if the app is running.
        /// </summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// Header to include in emitted log messages.  
        /// Default is [RabbitMqApp].
        /// </summary>
        public string Header
        {
            get
            {
                return _Header;
            }
            set
            {
                if (!String.IsNullOrEmpty(value) && !value.EndsWith(" ")) value += " ";
                if (String.IsNullOrEmpty(value)) _Header = "";
                else _Header = value;
            }
        }

        #endregion

        #region Private-Members

        private SwiftStackApp _App = null;

        private string _Header = "[RabbitMqApp] ";
        private CancellationTokenSource _TokenSource;
        private CancellationToken _Token;

        private List<object> _Interfaces = new List<object>();
        private readonly object _InterfaceLock = new object();

        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// SwiftStack RabbitMQ application.
        /// </summary>
        /// <param name="app">SwiftStack app.</param>
        public RabbitMqApp(SwiftStackApp app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            _App = app;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Run the application.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Run(CancellationToken token = default)
        {
            _TokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            _Token = token;

            IsRunning = true;

            while (true)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (token.IsCancellationRequested) break;
            }

            IsRunning = false;

            _App.Logging.Info(_Header + "RabbitMQ application stopped");
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual async void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    _TokenSource.Cancel();
                    _TokenSource.Dispose();
                }

                _Disposed = true;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Add a broadcaster.
        /// </summary>
        /// <typeparam name="T">Type of object.  Must be JSON serializable.</typeparam>
        /// <param name="broadcaster">Broadcaster.</param>
        public void AddBroadcaster<T>(RabbitMqBroadcaster<T> broadcaster) where T : class
        {
            if (broadcaster == null) return;

            lock (_InterfaceLock)
            {
                _Interfaces.Add(broadcaster);
            }
        }

        /// <summary>
        /// Add a resilient broadcaster.
        /// Resilient interfaces include automatic reconnection and store-and-forward message handling with local disk persistence.
        /// Resilient interfaces should not be used for large objects or extremely high volume applications.
        /// </summary>
        /// <typeparam name="T">Type of object.  Must be JSON serializable.</typeparam>
        /// <param name="broadcaster">Broadcaster.</param>
        public void AddResilientBroadcaster<T>(ResilientRabbitMqBroadcaster<T> broadcaster) where T : class
        {
            if (broadcaster == null) return;

            lock (_InterfaceLock)
            {
                _Interfaces.Add(broadcaster);
            }
        }

        /// <summary>
        /// Add a broadcast receiver.
        /// </summary>
        /// <typeparam name="T">Type of object.  Must be JSON serializable.</typeparam>
        /// <param name="receiver">Broadcast receiver.</param>
        public void AddBroadcastReceiver<T>(RabbitMqBroadcastReceiver<T> receiver) where T : class
        {
            if (receiver == null) return;

            lock (_InterfaceLock)
            {
                _Interfaces.Add(receiver);
            }
        }

        /// <summary>
        /// Add a resilient broadcast receiver.
        /// Resilient interfaces include automatic reconnection and store-and-forward message handling with local disk persistence.
        /// Resilient interfaces should not be used for large objects or extremely high volume applications.
        /// </summary>
        /// <typeparam name="T">Type of object.  Must be JSON serializable.</typeparam>
        /// <param name="receiver">Broadcast receiver.</param>
        public void AddResilientBroadcastReceiver<T>(ResilientRabbitMqBroadcastReceiver<T> receiver) where T : class
        {
            if (receiver == null) return;

            lock (_InterfaceLock)
            {
                _Interfaces.Add(receiver);
            }
        }

        /// <summary>
        /// Add a producer.
        /// </summary>
        /// <typeparam name="T">Type of object.  Must be JSON serializable.</typeparam>
        /// <param name="producer">Producer</param>
        public void AddProducer<T>(RabbitMqProducer<T> producer) where T : class
        {
            if (producer == null) return;

            lock (_InterfaceLock)
            {
                _Interfaces.Add(producer);
            }
        }

        /// <summary>
        /// Add a resilient producer.
        /// Resilient interfaces include automatic reconnection and store-and-forward message handling with local disk persistence.
        /// Resilient interfaces should not be used for large objects or extremely high volume applications.
        /// </summary>
        /// <typeparam name="T">Type of object.  Must be JSON serializable.</typeparam>
        /// <param name="producer">Producer</param>
        public void AddResilientProducer<T>(ResilientRabbitMqProducer<T> producer) where T : class
        {
            if (producer == null) return;

            lock (_InterfaceLock)
            {
                _Interfaces.Add(producer);
            }
        }

        /// <summary>
        /// Add a consumer.
        /// </summary>
        /// <typeparam name="T">Type of object.  Must be JSON serializable.</typeparam>
        /// <param name="consumer">Consumer.</param>
        public void AddConsumer<T>(RabbitMqConsumer<T> consumer) where T : class
        {
            if (consumer == null) return;

            lock (_InterfaceLock)
            {
                _Interfaces.Add(consumer);
            }
        }

        /// <summary>
        /// Add a resilient consumer.
        /// Resilient interfaces include automatic reconnection and store-and-forward message handling with local disk persistence.
        /// Resilient interfaces should not be used for large objects or extremely high volume applications.
        /// </summary>
        /// <typeparam name="T">Type of object.  Must be JSON serializable.</typeparam>
        /// <param name="consumer">Consumer.</param>
        public void AddResilientConsumer<T>(ResilientRabbitMqConsumer<T> consumer) where T : class
        {
            if (consumer == null) return;

            lock (_InterfaceLock)
            {
                _Interfaces.Add(consumer);
            }
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
