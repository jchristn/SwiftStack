namespace SwiftStack.Websockets
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using SerializationHelper;
    using SyslogLogging;
    using WatsonWebsocket;

    /// <summary>
    /// Error response for websocket messages.
    /// </summary>
    internal class WebsocketErrorResponse
    {
        public bool error { get; set; }
        public string message { get; set; }
        public string details { get; set; }
        public string type { get; set; }
    }

    /// <summary>
    /// SwiftStack websockets application.
    /// </summary>
    public class WebsocketsApp : IDisposable
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        /// <summary>
        /// Boolean to indicate if the app is running.
        /// </summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// Header to include in emitted log messages.  
        /// Default is [RestApp].
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

        /// <summary>
        /// Set to true to disable log messages on startup.
        /// </summary>
        public bool QuietStartup { get; set; } = false;

        /// <summary>
        /// Websocket settings.
        /// </summary>
        public WebsocketSettings WebsocketSettings
        {
            get
            {
                return _WebsocketSettings;
            }
            set
            {
                if (value == null) value = new WebsocketSettings();
                _WebsocketSettings = value;
            }
        }

        /// <summary>
        /// Websocket server.
        /// </summary>
        public WatsonWsServer WebsocketServer
        {
            get
            {
                return _WebsocketServer;
            }
        }

        /// <summary>
        /// Event to fire when a client connects.
        /// </summary>
        public EventHandler<ConnectionEventArgs> OnConnection { get; set; }

        /// <summary>
        /// Event to fire when a client disconnects.
        /// </summary>
        public EventHandler<DisconnectionEventArgs> OnDisconnection { get; set; }

        /// <summary>
        /// Default route, used when no matching route handler is found.
        /// </summary>
        public EventHandler<WebsocketsMessage> DefaultRoute { get; set; }

        /// <summary>
        /// Not found route, used when a message specifies a route that doesn't exist.
        /// If not set, falls back to DefaultRoute.
        /// </summary>
        public EventHandler<WebsocketsMessage> NotFoundRoute { get; set; }

        /// <summary>
        /// Exception route.
        /// </summary>
        public Func<WebsocketsMessage, Exception, CancellationToken, Task> ExceptionRoute { get; set; }

        /// <summary>
        /// When true and no ExceptionRoute is set, sends a default error message to the client.
        /// Default is false for backward compatibility.
        /// </summary>
        public bool SendExceptionMessagesToClient { get; set; } = false;

        /// <summary>
        /// When true, includes exception details in default error messages sent to clients.
        /// Only applies when SendExceptionMessagesToClient is true and no ExceptionRoute is set.
        /// Default is false for security.
        /// </summary>
        public bool IncludeExceptionDetailsInClientMessages { get; set; } = false;

        #endregion

        #region Private-Members

        private SwiftStackApp _App = null;

        private string _Header = "[WebsocketApp] ";
        private WebsocketSettings _WebsocketSettings = null;
        private WatsonWsServer _WebsocketServer = null;
        private CancellationTokenSource _TokenSource;
        private CancellationToken _Token;
        private Task _WebsocketTask = null;

        private Dictionary<string, Func<WebsocketsMessage, CancellationToken, Task>> _Routes = new Dictionary<string, Func<WebsocketsMessage, CancellationToken, Task>>();
        private readonly object _RoutesLock = new object();

        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// SwiftStack websockets application.
        /// </summary>
        /// <param name="app">SwiftStack app.</param>
        public WebsocketsApp(SwiftStackApp app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            _App = app;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Add a route.  
        /// </summary>
        /// <param name="name">Name of the route.</param>
        /// <param name="handler">Method to invoke.</param>
        public void AddRoute(string name, Func<WebsocketsMessage, CancellationToken, Task> handler)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_RoutesLock)
                _Routes.Add(name, handler);
        }

        /// <summary>
        /// Run the application.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Run(CancellationToken token = default)
        {
            _TokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            _Token = token;

            _WebsocketServer = new WatsonWsServer(_WebsocketSettings);
            _WebsocketServer.ClientConnected += OnConnection;
            _WebsocketServer.ClientDisconnected += OnDisconnection;
            _WebsocketServer.MessageReceived += MessageHandler;

            _WebsocketTask = _WebsocketServer.StartAsync(_Token);

            IsRunning = true;

            while (true)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (token.IsCancellationRequested) break;
            }

            IsRunning = false;

            _App.Logging.Info(_Header + "websockets application stopped");
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
                    if (_WebsocketServer != null && _WebsocketServer.IsListening)
                        _WebsocketServer?.Stop();

                    _TokenSource.Cancel();

                    // Wait for the websocket task with a timeout to avoid hanging
                    if (_WebsocketTask != null)
                    {
                        _WebsocketTask.Wait(TimeSpan.FromMilliseconds(500));
                    }

                    _TokenSource.Dispose();
                }

                _WebsocketSettings = null;
                _WebsocketTask = null;
                _WebsocketServer = null;

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

        #endregion

        #region Private-Methods

        private async void MessageHandler(object sender, MessageReceivedEventArgs e)
        {
            WebsocketsMessage msg = null;

            try
            {
                msg = new WebsocketsMessage
                {
                    // these values are from the websockets library
                    Payload = e.MessageType,
                    Ip = e.Client.Ip,
                    Port = e.Client.Port,
                    Name = e.Client.Name,
                    Data = e.Data
                };

                // Store reference to the server for response helper
                msg.SetWebsocketServer(_WebsocketServer);

                if (e.Data != null && e.Data.Count > 0)
                {
                    try
                    {
                        WebsocketsMessage payload = _App.Serializer.DeserializeJson<WebsocketsMessage>(e.Data.ToArray());

                        // these values are taken from the payload
                        msg.GUID = payload.GUID;
                        msg.Sender = payload.Sender;
                        msg.Conversation = payload.Conversation;
                        msg.ReplyTo = payload.ReplyTo;
                        msg.Route = payload.Route;

                        // if the payload has data, use it; otherwise keep the raw data
                        if (payload.Data.Array != null && payload.Data.Count > 0)
                        {
                            msg.Data = payload.Data;
                        }
                    }
                    catch (JsonException)
                    {
                        // ignore in case they're not using the framing format
                    }
                }

                Func<WebsocketsMessage, CancellationToken, Task> route = null;
                bool routeFound = false;

                if (!String.IsNullOrEmpty(msg.Route))
                {
                    lock (_RoutesLock)
                    {
                        if (_Routes.Any(r => r.Key.Equals(msg.Route)))
                        {
                            route = _Routes[msg.Route];
                            routeFound = true;
                        }
                    }
                }

                if (route != null)
                {
                    await route(msg, _Token).ConfigureAwait(false);
                }
                else if (!String.IsNullOrEmpty(msg.Route) && !routeFound)
                {
                    // Route was specified but not found
                    _App.Logging.Debug(_Header + "route not found for message " + msg.GUID + " (route: " + msg.Route + ")");

                    if (NotFoundRoute != null)
                    {
                        NotFoundRoute.Invoke(this, msg);
                    }
                    else if (DefaultRoute != null)
                    {
                        // Fall back to DefaultRoute if NotFoundRoute is not set
                        DefaultRoute.Invoke(this, msg);
                    }
                }
                else
                {
                    // No route specified - use default route
                    _App.Logging.Debug(_Header + "no route specified for message " + msg.GUID);
                    DefaultRoute?.Invoke(this, msg);
                }
            }
            catch (Exception ex)
            {
                _App.Logging.Warn(_Header + "exception in message handler:" + Environment.NewLine + ex.ToString());

                if (ExceptionRoute != null)
                {
                    await ExceptionRoute(msg, ex, _Token).ConfigureAwait(false);
                }
                else if (SendExceptionMessagesToClient && msg != null)
                {
                    // Send default error response to client
                    try
                    {
                        WebsocketErrorResponse errorResponse = new WebsocketErrorResponse
                        {
                            error = true,
                            message = "An error occurred processing your request",
                            details = IncludeExceptionDetailsInClientMessages ? ex.Message : null,
                            type = IncludeExceptionDetailsInClientMessages ? ex.GetType().Name : null
                        };

                        await msg.RespondAsync(errorResponse).ConfigureAwait(false);
                    }
                    catch (Exception sendEx)
                    {
                        _App.Logging.Warn(_Header + "failed to send exception message to client: " + sendEx.Message);
                    }
                }
            }
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}