namespace SwiftStack
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using SerializationHelper;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// SwiftStack application.
    /// </summary>
    public class SwiftStackApp : IDisposable
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        /// <summary>
        /// Name of the application.
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (!String.IsNullOrEmpty(value)) _Name = value;
            }
        }

        /// <summary>
        /// String to prepend to log messages.  
        /// A space character will automatically be appended to the end if 
        /// the value supplied doesn't end with a space character.
        /// </summary>
        public string LogHeader
        {
            get
            {
                return _LogHeader;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) value = "";
                if (!value.EndsWith(" ")) value += " ";
                _LogHeader = value;
            }
        }

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings LoggingSettings
        {
            get
            {
                return _LoggingSettings;
            }
            set
            {
                if (value == null) value = new LoggingSettings();
                _LoggingSettings = value;
            }
        }

        /// <summary>
        /// Logging servers.
        /// </summary>
        public List<SyslogServer> LoggingServers
        {
            get
            {
                return _LoggingServers;
            }
        }

        /// <summary>
        /// Logger.
        /// </summary>
        public LoggingModule Logger
        {
            get
            {
                return _Logger;
            }
        }

        /// <summary>
        /// Webserver settings.
        /// </summary>
        public WebserverSettings WebserverSettings
        {
            get
            {
                return _WebserverSettings;
            }
            set
            {
                if (value == null) value = new WebserverSettings();
                _WebserverSettings = value;
            }
        }

        /// <summary>
        /// Webserver.
        /// </summary>
        public Webserver Webserver
        {
            get
            {
                return _Webserver;
            }
        }

        /// <summary>
        /// Exception route.
        /// </summary>
        public Func<HttpContextBase, Exception, Task> ExceptionRoute
        {
            get
            {
                return _ExceptionRoute;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(ExceptionRoute));
                _ExceptionRoute = value;
            }
        }

        /// <summary>
        /// JSON serializer.
        /// </summary>
        public Serializer Serializer
        {
            get
            {
                return _Serializer;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Serializer));
                _Serializer = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Name = "SwiftStack Application";

        private string _LogHeader = "[SwiftStackApp] ";
        private LoggingSettings _LoggingSettings = new LoggingSettings();
        private List<SyslogServer> _LoggingServers = new List<SyslogServer>
        {
            new SyslogServer()
        };
        private LoggingModule _Logger = null;
        private Serializer _Serializer = new Serializer();

        private Dictionary<string, RouteInfo> _RouteTypes = new Dictionary<string, RouteInfo>();
        private List<ParameterRoute> _AuthenticatedRoutes = new List<ParameterRoute>();
        private List<ParameterRoute> _UnauthenticatedRoutes = new List<ParameterRoute>();
        private WebserverSettings _WebserverSettings = new WebserverSettings();
        private Webserver _Webserver = null;
        private Func<HttpContextBase, Exception, Task> _ExceptionRoute;

        private Task _WebserverTask;
        private CancellationTokenSource _TokenSource;
        private CancellationToken _Token;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public SwiftStackApp()
        {
            try
            {
                Console.WriteLine(
                    Environment.NewLine + Constants.Logo + 
                    Environment.NewLine + Constants.Copyright + 
                    Environment.NewLine);
            }
            catch { }

            _ExceptionRoute = DefaultExceptionRoute;
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

            _WebserverTask = Task.Run(async () =>
            {
                try
                {
                    using (_Logger = new LoggingModule(_LoggingServers, _LoggingSettings.EnableConsole))
                    {
                        _Logger.Settings = _LoggingSettings;

                        using (_Webserver = new Webserver(_WebserverSettings, DefaultRoute))
                        {
                            Log(Severity.Info, "starting SwiftStack webserver on " + _WebserverSettings.Prefix);

                            foreach (ParameterRoute authenticatedRoute in _AuthenticatedRoutes)
                            {
                                _Webserver.Routes.PostAuthentication.Parameter.Add(
                                    authenticatedRoute.Method,
                                    authenticatedRoute.Path,
                                    authenticatedRoute.Handler,
                                    ExceptionRoute);
                                Log(Severity.Debug, "added authenticated route " + authenticatedRoute.Method + " " + authenticatedRoute.Path);
                            }

                            foreach (ParameterRoute unauthenticatedRoute in _UnauthenticatedRoutes)
                            {
                                _Webserver.Routes.PreAuthentication.Parameter.Add(
                                    unauthenticatedRoute.Method,
                                    unauthenticatedRoute.Path,
                                    unauthenticatedRoute.Handler,
                                    ExceptionRoute);
                                Log(Severity.Debug, "added route " + unauthenticatedRoute.Method + " " + unauthenticatedRoute.Path);
                            }

                            _Webserver.Routes.PreRouting = PreRoutingRoute;
                            _Webserver.Routes.PostRouting = PostRoutingRoute;

                            await _Webserver.StartAsync(token);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log(
                        Severity.Alert,
                        "exception starting webserver on " + _WebserverSettings.Prefix + Environment.NewLine +
                        e.ToString());
                }
            });

            while (true)
            {
                await Task.Delay(100).ConfigureAwait(false);
                if (token.IsCancellationRequested) break;
            }

            Log(Severity.Info, "SwiftStack stopped");
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
                    _WebserverTask.Wait();
                    _TokenSource.Dispose();
                }

                _Name = null;
                _LogHeader = null;
                _Logger = null;
                _LoggingSettings = null;
                _LoggingServers = null;
                _WebserverSettings = null;
                _Webserver = null;

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
        /// Adds a route handler for requests with no request body and no response body.
        /// </summary>
        /// <param name="method">HTTP method (GET, POST, etc)</param>
        /// <param name="path">URL path starting with /</param>
        /// <param name="handler">Request handler</param>
        /// <param name="requiresAuthentication">Whether authentication is required</param>
        public void Route(
            string method,
            string path,
            Func<AppRequest<object>, Task<AppResponse>> handler,
            bool requiresAuthentication = false)
        {
            AddRouteInternal(method, path, handler, requiresAuthentication);
        }

        /// <summary>
        /// Adds a route handler for requests with no request body but returns a response body.
        /// </summary>
        /// <typeparam name="TResponse">Type of response data</typeparam>
        /// <param name="method">HTTP method (GET, POST, etc)</param>
        /// <param name="path">URL path starting with /</param>
        /// <param name="handler">Request handler</param>
        /// <param name="requiresAuthentication">Whether authentication is required</param>
        public void Route<TResponse>(
            string method,
            string path,
            Func<AppRequest<object>, Task<AppResponse<TResponse>>> handler,
            bool requiresAuthentication = false)
        {
            AddRouteInternal(method, path, handler, requiresAuthentication);
        }

        /// <summary>
        /// Adds a route handler for requests with no data exchange.
        /// </summary>
        /// <param name="method">HTTP method (GET, POST, etc)</param>
        /// <param name="path">URL path starting with /</param>
        /// <param name="handler">Request handler</param>
        /// <param name="requiresAuthentication">Whether authentication is required</param>
        public void Route(
            string method,
            string path,
            Func<Task<AppResponse>> handler,
            bool requiresAuthentication = false)
        {
            AddRouteInternal(method, path, async (req) => await handler(), requiresAuthentication);
        }

        /// <summary>
        /// Adds a route handler for requests that return data but take no input.
        /// </summary>
        /// <typeparam name="TResponse">Type of response data</typeparam>
        /// <param name="method">HTTP method (GET, POST, etc)</param>
        /// <param name="path">URL path starting with /</param>
        /// <param name="handler">Request handler</param>
        /// <param name="requiresAuthentication">Whether authentication is required</param>
        public void Route<TResponse>(
            string method,
            string path,
            Func<Task<AppResponse<TResponse>>> handler,
            bool requiresAuthentication = false)
        {
            AddRouteInternal(method, path, async (req) => await handler(), requiresAuthentication);
        }

        /// <summary>
        /// Adds a route handler for requests with both request and response bodies.
        /// </summary>
        /// <typeparam name="TRequest">Type of request data</typeparam>
        /// <typeparam name="TResponse">Type of response data</typeparam>
        /// <param name="method">HTTP method (GET, POST, etc)</param>
        /// <param name="path">URL path starting with /</param>
        /// <param name="handler">Request handler</param>
        /// <param name="requiresAuthentication">Whether authentication is required</param>
        public void Route<TRequest, TResponse>(
            string method,
            string path,
            Func<AppRequest<TRequest>, Task<AppResponse<TResponse>>> handler,
            bool requiresAuthentication = false)
            where TRequest : class
        {
            AddRouteInternal(method, path, handler, requiresAuthentication);
        }

        /// <summary>
        /// Adds a GET route handler that returns data.
        /// </summary>
        /// <typeparam name="TResponse">Type of response data</typeparam>
        /// <param name="path">URL path starting with /</param>
        /// <param name="handler">Request handler</param>
        /// <param name="requiresAuthentication">Whether authentication is required</param>
        public void Get<TResponse>(
            string path,
            Func<AppRequest<object>, Task<AppResponse<TResponse>>> handler,
            bool requiresAuthentication = false)
        {
            Route<TResponse>("GET", path, handler, requiresAuthentication);
        }

        /// <summary>
        /// Adds a POST route handler that accepts and returns data.
        /// </summary>
        /// <typeparam name="TRequest">Type of request data</typeparam>
        /// <typeparam name="TResponse">Type of response data</typeparam>
        /// <param name="path">URL path starting with /</param>
        /// <param name="handler">Request handler</param>
        /// <param name="requiresAuthentication">Whether authentication is required</param>
        public void Post<TRequest, TResponse>(
            string path,
            Func<AppRequest<TRequest>, Task<AppResponse<TResponse>>> handler,
            bool requiresAuthentication = false)
            where TRequest : class
        {
            Route<TRequest, TResponse>("POST", path, handler, requiresAuthentication);
        }

        /// <summary>
        /// Adds a PUT route handler that accepts and returns data.
        /// </summary>
        /// <typeparam name="TRequest">Type of request data</typeparam>
        /// <typeparam name="TResponse">Type of response data</typeparam>
        /// <param name="path">URL path starting with /</param>
        /// <param name="handler">Request handler</param>
        /// <param name="requiresAuthentication">Whether authentication is required</param>
        public void Put<TRequest, TResponse>(
            string path,
            Func<AppRequest<TRequest>, Task<AppResponse<TResponse>>> handler,
            bool requiresAuthentication = false)
            where TRequest : class
        {
            Route<TRequest, TResponse>("PUT", path, handler, requiresAuthentication);
        }

        /// <summary>
        /// Adds a DELETE route handler.
        /// </summary>
        /// <param name="path">URL path starting with /</param>
        /// <param name="handler">Request handler</param>
        /// <param name="requiresAuthentication">Whether authentication is required</param>
        public void Delete(
            string path,
            Func<AppRequest<object>, Task<AppResponse>> handler,
            bool requiresAuthentication = false)
        {
            Route("DELETE", path, handler, requiresAuthentication);
        }

        /// <summary>
        /// Adds a HEAD route handler.
        /// </summary>
        /// <param name="path">URL path starting with /</param>
        /// <param name="handler">Request handler</param>
        /// <param name="requiresAuthentication">Whether authentication is required</param>
        public void Head(
            string path,
            Func<AppRequest<object>, Task<AppResponse>> handler,
            bool requiresAuthentication = false)
        {
            Route("HEAD", path, handler, requiresAuthentication);
        }

        #endregion

        #region Private-Methods

        private void Log(Severity sev, string msg)
        {
            if (!String.IsNullOrWhiteSpace(msg)) _Logger.Log(sev, _LogHeader + msg);
        }

        private void AddRouteInternal(
            string method,
            string path,
            Func<AppRequest<object>, Task<AppResponse>> handler,
            bool requiresAuthentication)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!path.StartsWith("/")) throw new ArgumentException("Paths must start with /.");

            var httpMethod = (HttpMethod)Enum.Parse(typeof(HttpMethod), method.ToUpper());

            Func<HttpContextBase, Task> wrappedHandler = async (ctx) =>
            {
                try
                {
                    var request = new AppRequest<object>(ctx, _Serializer, false);
                    var response = await handler(request);

                    if (response.StatusCode.HasValue)
                    {
                        ctx.Response.StatusCode = response.StatusCode.Value;
                    }

                    foreach (string key in response.Headers.Keys)
                    {
                        ctx.Response.Headers[key] = response.Headers[key];
                    }
                }
                catch (SwiftStackException ex)
                {
                    ctx.Response.StatusCode = GetStatusCodeForResult(ex.Result);
                    var errorResponse = new AppResponse
                    {
                        Result = ex.Result,
                        StatusCode = ctx.Response.StatusCode
                    };
                    var errorJson = _Serializer.SerializeJson(errorResponse);
                    await ctx.Response.Send(errorJson);
                }
                catch (Exception e)
                {
                    Log(Severity.Warn, "uncaught exception of type " + e.GetType().ToString() + " in route " + method + " " + path + ":" + Environment.NewLine + e.ToString());
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send();
                }
            };

            var routes = requiresAuthentication ? _AuthenticatedRoutes : _UnauthenticatedRoutes;
            routes.Add(new ParameterRoute(httpMethod, path, wrappedHandler, ExceptionRoute));
        }

        // For no request body, with response body
        private void AddRouteInternal<TResponse>(
            string method,
            string path,
            Func<AppRequest<object>, Task<AppResponse<TResponse>>> handler,
            bool requiresAuthentication)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!path.StartsWith("/")) throw new ArgumentException("Paths must start with /.");

            var httpMethod = (HttpMethod)Enum.Parse(typeof(HttpMethod), method.ToUpper());

            Func<HttpContextBase, Task> wrappedHandler = async (ctx) =>
            {
                try
                {
                    AppRequest<object> req = new AppRequest<object>(ctx, _Serializer, false);
                    AppResponse resp = await handler(req);

                    if (resp.StatusCode.HasValue)
                    {
                        ctx.Response.StatusCode = resp.StatusCode.Value;
                    }

                    foreach (string key in resp.Headers.Keys)
                    {
                        ctx.Response.Headers[key] = resp.Headers[key];
                    }

                    if (resp.Data != null)
                    {
                        bool isPrimitive = typeof(TResponse) == typeof(string) ||
                                         (typeof(TResponse) != null && typeof(TResponse).IsPrimitive);

                        if (String.IsNullOrEmpty(ctx.Response.Headers["Content-Type"]))
                        {
                            ctx.Response.Headers.Add("Content-Type", isPrimitive ? "text/plain" : "application/json");
                        }

                        await WriteResponse(ctx, resp);
                    }
                    else
                    {
                        await WriteResponse(ctx, resp);
                    }
                }
                catch (SwiftStackException ex)
                {
                    ctx.Response.StatusCode = GetStatusCodeForResult(ex.Result);
                    var errorResponse = new AppResponse
                    {
                        Result = ex.Result,
                        StatusCode = ctx.Response.StatusCode
                    };
                    var errorJson = _Serializer.SerializeJson(errorResponse);
                    await ctx.Response.Send(errorJson);
                }
                catch (Exception e)
                {
                    Log(Severity.Warn, "uncaught exception of type " + e.GetType().ToString() + " in route " + method + " " + path + ":" + Environment.NewLine + e.ToString());
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send();
                }
            };

            var routes = requiresAuthentication ? _AuthenticatedRoutes : _UnauthenticatedRoutes;
            routes.Add(new ParameterRoute(httpMethod, path, wrappedHandler, ExceptionRoute));
        }

        // For request body and response body
        private void AddRouteInternal<TRequest, TResponse>(
            string method,
            string path,
            Func<AppRequest<TRequest>, Task<AppResponse<TResponse>>> handler,
            bool requiresAuthentication) where TRequest : class
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!path.StartsWith("/")) throw new ArgumentException("Paths must start with /.");

            var httpMethod = (HttpMethod)Enum.Parse(typeof(HttpMethod), method.ToUpper());

            Func<HttpContextBase, Task> wrappedHandler = async (ctx) =>
            {
                try
                {
                    bool isPrimitive = typeof(TRequest) == typeof(string) ||
                                     (typeof(TRequest) != null && typeof(TRequest).IsPrimitive);

                    AppRequest<TRequest> req = new AppRequest<TRequest>(ctx, _Serializer, isPrimitive);

                    AppResponse resp = await handler(req);

                    if (resp.StatusCode.HasValue)
                    {
                        ctx.Response.StatusCode = resp.StatusCode.Value;
                    }

                    foreach (string key in resp.Headers.Keys)
                    {
                        ctx.Response.Headers[key] = resp.Headers[key];
                    }

                    if (resp.Data != null)
                    {
                        bool isResponsePrimitive = typeof(TResponse) == typeof(string) ||
                                                (typeof(TResponse) != null && typeof(TResponse).IsPrimitive);

                        if (String.IsNullOrEmpty(ctx.Response.Headers["Content-Type"]))
                        {
                            ctx.Response.Headers.Add("Content-Type", isResponsePrimitive ? "text/plain" : "application/json");
                        }

                        await WriteResponse(ctx, resp);
                    }
                    else
                    {
                        await WriteResponse(ctx, resp);
                    }
                }
                catch (SwiftStackException ex)
                {
                    ctx.Response.StatusCode = GetStatusCodeForResult(ex.Result);
                    var errorResponse = new AppResponse
                    {
                        Result = ex.Result,
                        StatusCode = ctx.Response.StatusCode
                    };
                    var errorJson = _Serializer.SerializeJson(errorResponse);
                    await ctx.Response.Send(errorJson);
                }
                catch (Exception e)
                {
                    Log(Severity.Warn, "uncaught exception of type " + e.GetType().ToString() + " in route " + method + " " + path + ":" + Environment.NewLine + e.ToString());
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send();
                }
            };

            var routes = requiresAuthentication ? _AuthenticatedRoutes : _UnauthenticatedRoutes;
            routes.Add(new ParameterRoute(httpMethod, path, wrappedHandler, ExceptionRoute));
        }

        private async Task DefaultRoute(HttpContextBase ctx)
        {
            string requestor = ctx.Request.Source.IpAddress + ":" + ctx.Request.Source.Port;

            Log(Severity.Debug, "invalid method or URL in request from " + requestor + ": " + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery);

            ctx.Response.StatusCode = 400;
            await ctx.Response.Send();
        }

        private async Task PreRoutingRoute(HttpContextBase ctx)
        {
            
        }

        private async Task PostRoutingRoute(HttpContextBase ctx)
        {
            ctx.Request.Timestamp.End = DateTime.UtcNow;
            Log(Severity.Debug, 
                ctx.Request.Method + " " + 
                ctx.Request.Url.RawWithQuery + ": " + 
                ctx.Response.StatusCode + " " +
                "(" + ctx.Request.Timestamp.TotalMs.Value.ToString("0.00") + "ms)");
        }

        private async Task DefaultExceptionRoute(HttpContextBase ctx, Exception e)
        {
            Log(Severity.Alert, "exception encountered:" + Environment.NewLine + e.ToString());
            ctx.Response.StatusCode = 500;
            await ctx.Response.Send();
        }

        private int GetStatusCodeForResult(ApiResultEnum result)
        {
            switch (result)
            {
                case ApiResultEnum.Success:
                    return 200;
                case ApiResultEnum.Created:
                    return 201;
                case ApiResultEnum.NotFound:
                    return 404;
                case ApiResultEnum.NotAuthorized:
                    return 401;
                case ApiResultEnum.InternalError:
                    return 500;
                case ApiResultEnum.SlowDown:
                    return 429;
                case ApiResultEnum.Conflict:
                    return 409;
                default:
                    return 500;
            };
        }

        private async Task WriteResponse(HttpContextBase ctx, AppResponse resp)
        {
            try
            {
                ctx.Response.StatusCode = resp.StatusCode.Value;

                // Check if this is a generic AppResponse<T>
                Type respType = resp.GetType();
                bool isGeneric = respType.IsGenericType && respType.GetGenericTypeDefinition() == typeof(AppResponse<>);

                object data = null;
                if (isGeneric)
                {
                    // Get the Data property from the specific generic type
                    var properties = respType.GetProperties();
                    var dataProp = properties.FirstOrDefault(p => p.Name == "Data" && p.DeclaringType != typeof(AppResponse));
                    if (dataProp != null)
                    {
                        data = dataProp.GetValue(resp);
                    }
                }
                else
                {
                    data = resp.Data;
                }

                if (data == null)
                {
                    await ctx.Response.Send();
                    return;
                }

                if (data is string || data.GetType().IsPrimitive)
                {
                    await ctx.Response.Send(data.ToString());
                    return;
                }
                else
                {
                    string json = _Serializer.SerializeJson(data, false);
                    await ctx.Response.Send(json);
                    return;
                }
            }
            catch (Exception e)
            {
                Log(Severity.Warn, "exception in write response:" + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(e.Message);
            }
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
