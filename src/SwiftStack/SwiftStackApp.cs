namespace SwiftStack
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
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
        /// Boolean to indicate if the app is running.
        /// </summary>
        public bool IsRunning { get; private set; } = false;

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
            set
            {
                _Logger = value;
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
        /// Authentication route.
        /// </summary>
        public Func<HttpContextBase, Task<AuthResult>> AuthenticationRoute { get; set; } = null;

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

        #region Public-General-Methods

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
                    if (_Logger == null)
                    {
                        _Logger = new LoggingModule(_LoggingServers, _LoggingSettings.EnableConsole);
                        _Logger.Settings = _LoggingSettings;
                        _Logger.Settings.HeaderFormat = "{ts} {sev}";
                    }

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
                        if (AuthenticationRoute != null) _Webserver.Routes.AuthenticateRequest = AuthenticateRequest;

                        await _Webserver.StartAsync(token);
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

            IsRunning = true;

            while (true)
            {
                await Task.Delay(100).ConfigureAwait(false);
                if (token.IsCancellationRequested) break;
            }

            IsRunning = false;

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

        #endregion

        #region Public-Route-Methods

        /// <summary>
        /// Add a GET route.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="handler">Route body.</param>
        /// <param name="requireAuthentication">True to require authentication.</param>
        public void Get(
            string path,
            Func<AppRequest, Task<object>> handler,
            bool requireAuthentication = false)
        {
            RegisterNoBodyRoute(HttpMethod.GET, path, handler, requireAuthentication);
        }

        /// <summary>
        /// Add a POST route.
        /// </summary>
        /// <typeparam name="T">Request body type.</typeparam>
        /// <param name="path">Path.</param>
        /// <param name="handler">Route body.</param>
        /// <param name="requireAuthentication">True to require authentication.</param>
        public void Post<T>(
            string path,
            Func<AppRequest, Task<object>> handler,
            bool requireAuthentication = false) where T : class
        {
            RegisterBodyRoute<T>(HttpMethod.POST, path, handler, requireAuthentication);
        }

        /// <summary>
        /// Add a PUT route.
        /// </summary>
        /// <typeparam name="T">Request body type.</typeparam>
        /// <param name="path">Path.</param>
        /// <param name="handler">Route body.</param>
        /// <param name="requireAuthentication">True to require authentication.</param>
        public void Put<T>(
            string path,
            Func<AppRequest, Task<object>> handler,
            bool requireAuthentication = false) where T : class
        {
            RegisterBodyRoute<T>(HttpMethod.PUT, path, handler, requireAuthentication);
        }

        /// <summary>
        /// Add a PATCH route.
        /// </summary>
        /// <typeparam name="T">Request body type.</typeparam>
        /// <param name="path">Path.</param>
        /// <param name="handler">Route body.</param>
        /// <param name="requireAuthentication">True to require authentication.</param>
        public void Patch<T>(
            string path,
            Func<AppRequest, Task<object>> handler,
            bool requireAuthentication = false) where T : class
        {
            RegisterBodyRoute<T>(HttpMethod.PATCH, path, handler, requireAuthentication);
        }

        /// <summary>
        /// Add a DELETE route.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="handler">Route body.</param>
        /// <param name="requireAuthentication">True to require authentication.</param>
        public void Delete(
            string path,
            Func<AppRequest, Task<object>> handler,
            bool requireAuthentication = false)
        {
            RegisterNoBodyRoute(HttpMethod.DELETE, path, handler, requireAuthentication);
        }

        /// <summary>
        /// Add a DELETE route.
        /// </summary>
        /// <typeparam name="T">Request body type.</typeparam>
        /// <param name="path">Path.</param>
        /// <param name="handler">Route body.</param>
        /// <param name="requireAuthentication">True to require authentication.</param>
        public void Delete<T>(
            string path,
            Func<AppRequest, Task<object>> handler,
            bool requireAuthentication = false) where T : class
        {
            RegisterBodyRoute<T>(HttpMethod.DELETE, path, handler, requireAuthentication);
        }

        /// <summary>
        /// Add a HEAD route.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="handler">Route body.</param>
        /// <param name="requireAuthentication">True to require authentication.</param>
        public void Head(
            string path,
            Func<AppRequest, Task<object>> handler,
            bool requireAuthentication = false)
        {
            RegisterNoBodyRoute(HttpMethod.HEAD, path, handler, requireAuthentication);
        }

        /// <summary>
        /// Add an OPTIONS route.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="handler">Route body.</param>
        /// <param name="requireAuthentication">True to require authentication.</param>
        public void Options(
            string path,
            Func<AppRequest, Task<object>> handler,
            bool requireAuthentication = false)
        {
            RegisterNoBodyRoute(HttpMethod.OPTIONS, path, handler, requireAuthentication);
        }

        /// <summary>
        /// Add a route.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="path">URL path.</param>
        /// <param name="handler">Route body.</param>
        /// <param name="requestType">Request body type.</param>
        /// <param name="requireAuthentication">True to require authentication.</param>
        public void Route(
            string method,
            string path,
            Func<AppRequest, Task<object>> handler,
            Type requestType = null,
            bool requireAuthentication = false)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!path.StartsWith("/")) throw new ArgumentException("Paths must start with /.");
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (requestType == null) requestType = typeof(object);

            HttpMethod httpMethod = (HttpMethod)Enum.Parse(typeof(HttpMethod), method.ToUpper());

            Func<HttpContextBase, Task> wrappedHandler = async (ctx) =>
            {
                try
                {
                    // Create request object based on requestType
                    object requestObj = null;
                    if (!String.IsNullOrEmpty(ctx.Request.DataAsString))
                    {
                        if (requestType == typeof(string))
                        {
                            requestObj = ctx.Request.DataAsString;
                        }
                        else if (requestType.IsPrimitive)
                        {
                            requestObj = Convert.ChangeType(ctx.Request.DataAsString, requestType);
                        }
                        else if (requestType == typeof(object))
                        {
                            // For raw object type, try to deserialize as dynamic JSON or use as string
                            try
                            {
                                requestObj = _Serializer.DeserializeJson<object>(ctx.Request.DataAsString);
                            }
                            catch
                            {
                                requestObj = ctx.Request.DataAsString;
                            }
                        }
                        else
                        {
                            // For specific types, we can't use a dynamic type parameter, 
                            // but we can leave requestObj as null and just pass the raw string
                            // The handler can deserialize it properly when needed
                            requestObj = ctx.Request.DataAsString;
                        }
                    }

                    AppRequest dynamicReq = new AppRequest(ctx, _Serializer, requestObj);
                    object result = await handler(dynamicReq);
                    await ProcessResult(ctx, result);
                }
                catch (Exception e)
                {
                    await HandleException(ctx, e);
                }
            };

            List<ParameterRoute> routes = requireAuthentication ? _AuthenticatedRoutes : _UnauthenticatedRoutes;
            routes.Add(new ParameterRoute(httpMethod, path, wrappedHandler, ExceptionRoute));
            Log(Severity.Debug, "added route " + method + " " + path);
        }

        #endregion

        #region Private-Methods

        private void Log(Severity sev, string msg)
        {
            if (!String.IsNullOrWhiteSpace(msg) && _Logger != null) _Logger.Log(sev, _LogHeader + msg);
        }

        private void RegisterRoute(
            HttpMethod method,
            string path,
            Func<HttpContextBase, Task> handler,
            bool requireAuthentication)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!path.StartsWith("/")) throw new ArgumentException("Paths must start with /.");
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            // Add route to appropriate collection
            List<ParameterRoute> routes = requireAuthentication ? _AuthenticatedRoutes : _UnauthenticatedRoutes;
            routes.Add(new ParameterRoute(method, path, handler, ExceptionRoute));
            Log(Severity.Debug, "added route " + method + " " + path);
        }

        private void RegisterNoBodyRoute(
            HttpMethod method,
            string path,
            Func<AppRequest, Task<object>> handler,
            bool requireAuthentication)
        {
            Func<HttpContextBase, Task> routeHandler = async (ctx) =>
            {
                try
                {
                    AppRequest dynamicReq = new AppRequest(ctx, _Serializer, null);
                    dynamicReq.Metadata = ctx.Metadata;

                    object result = await handler(dynamicReq);
                    await ProcessResult(ctx, result);
                }
                catch (Exception ex)
                {
                    await HandleException(ctx, ex);
                }
            };

            RegisterRoute(method, path, routeHandler, requireAuthentication);
        }

        private void RegisterBodyRoute<T>(
            HttpMethod method,
            string path,
            Func<AppRequest, Task<object>> handler,
            bool requireAuthentication) where T : class
        {
            Func<HttpContextBase, Task> routeHandler = async (ctx) =>
            {
                try
                {
                    T requestData = null;
                    if (!String.IsNullOrEmpty(ctx.Request.DataAsString))
                    {
                        requestData = _Serializer.DeserializeJson<T>(ctx.Request.DataAsString);
                    }

                    AppRequest dynamicReq = new AppRequest(ctx, _Serializer, requestData);
                    dynamicReq.Metadata = ctx.Metadata;

                    object result = await handler(dynamicReq);
                    await ProcessResult(ctx, result);
                }
                catch (Exception e)
                {
                    await HandleException(ctx, e);
                }
            };

            RegisterRoute(method, path, routeHandler, requireAuthentication);
        }

        private async Task DefaultRoute(HttpContextBase ctx)
        {
            string requestor = ctx.Request.Source.IpAddress + ":" + ctx.Request.Source.Port;
            Log(Severity.Debug, "invalid method or URL in request from " + requestor + ": " + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery);
            ctx.Response.StatusCode = 400;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse
            {
                Error = ApiResultEnum.BadRequest,
                Message = "Unable to find route associated with the supplied method and URL.",
            }, true));
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
        
        private async Task HandleException(HttpContextBase ctx, Exception e)
        {
            if (ExceptionRoute != null)
            {
                try
                {
                    await ExceptionRoute(ctx, e);
                }
                catch (Exception ex)
                {
                    ApiErrorResponse error = new ApiErrorResponse
                    {
                        Error = ApiResultEnum.InternalError,
                        Message = ex.Message,
                        Data = ex.Data
                    };

                    await ctx.Response.Send(_Serializer.SerializeJson(error, true));
                }
            }
            else if (e is SwiftStackException swiftEx)
            {
                ApiErrorResponse error = new ApiErrorResponse
                {
                    Error = swiftEx.Result,
                    Message = swiftEx.Message,
                    Data = swiftEx.Data
                };

                await ctx.Response.Send(_Serializer.SerializeJson(error, true));
            }
            else if (e is JsonException jsonEx)
            {
                ApiErrorResponse error = new ApiErrorResponse
                {
                    Error = ApiResultEnum.DeserializationError,
                    Message = jsonEx.Message,
                    Data = jsonEx.Data
                };

                await ctx.Response.Send(_Serializer.SerializeJson(error, true));
            }
            else
            {
                ApiErrorResponse error = new ApiErrorResponse
                {
                    Error = ApiResultEnum.InternalError,
                    Message = e.Message,
                    Data = e.Data
                };

                await ctx.Response.Send(_Serializer.SerializeJson(error, true));
            }
        }

        private async Task ProcessResult(HttpContextBase ctx, object result)
        {
            try
            {
                if (!ctx.Response.ServerSentEvents)
                {
                    // Handle null result
                    if (result == null)
                    {
                        ctx.Response.StatusCode = 204; // No Content
                        await ctx.Response.Send();
                        return;
                    }

                    // Handle string result
                    if (result is string stringResult)
                    {
                        ctx.Response.Headers.Add("Content-Type", "text/plain");
                        await ctx.Response.Send(stringResult);
                        return;
                    }

                    // Handle primitive result
                    if (result != null && result.GetType().IsPrimitive)
                    {
                        ctx.Response.Headers.Add("Content-Type", "text/plain");
                        await ctx.Response.Send(result.ToString());
                        return;
                    }

                    Type resultType = result.GetType();
                    if (resultType.Name.StartsWith("ValueTuple`"))
                    {
                        PropertyInfo item1Prop = resultType.GetProperty("Item1");
                        PropertyInfo item2Prop = resultType.GetProperty("Item2");

                        if (item1Prop != null && item2Prop != null)
                        {
                            object item1 = item1Prop.GetValue(result);
                            int statusCode = Convert.ToInt32(item2Prop.GetValue(result));

                            ctx.Response.StatusCode = statusCode;

                            if (item1 != null)
                            {
                                ctx.Response.Headers.Add("Content-Type", "application/json");
                                await ctx.Response.Send(_Serializer.SerializeJson(item1));
                            }
                            else
                            {
                                await ctx.Response.Send();
                            }

                            return;
                        }
                    }

                    // Handle Tuple with 2 items (data, statusCode)
                    if (result.GetType().IsGenericType && result.GetType().GetGenericTypeDefinition() == typeof(Tuple<,>))
                    {
                        PropertyInfo item1Prop = result.GetType().GetProperty("Item1");
                        PropertyInfo item2Prop = result.GetType().GetProperty("Item2");

                        if (item1Prop != null && item2Prop != null)
                        {
                            object item1 = item1Prop.GetValue(result);
                            int statusCode = Convert.ToInt32(item2Prop.GetValue(result));

                            ctx.Response.StatusCode = statusCode;

                            if (item1 != null)
                            {
                                if (item1 is string itemString)
                                {
                                    ctx.Response.Headers.Add("Content-Type", "text/plain");
                                    await ctx.Response.Send(itemString);
                                }
                                else if (item1.GetType().IsPrimitive)
                                {
                                    ctx.Response.Headers.Add("Content-Type", "text/plain");
                                    await ctx.Response.Send(item1.ToString());
                                }
                                else
                                {
                                    ctx.Response.Headers.Add("Content-Type", "application/json");
                                    await ctx.Response.Send(_Serializer.SerializeJson(item1));
                                }
                            }
                            else
                            {
                                await ctx.Response.Send();
                            }

                            return;
                        }
                    }

                    // Default: treat as JSON
                    ctx.Response.Headers.Add("Content-Type", "application/json");
                    await ctx.Response.Send(_Serializer.SerializeJson(result));
                }
                else
                {
                    // SSE responses are managed within the route implementation
                }
            }
            catch (Exception e)
            {
                await HandleException(ctx, e);
            }
        }

        private async Task AuthenticateRequest(HttpContextBase ctx)
        {
            ApiErrorResponse resp;

            try
            {
                AuthResult result = await AuthenticationRoute(ctx);

                if (result == null)
                {
                    #region No-Response

                    _Logger.Warn(_LogHeader + "no response from authentication route");
                    ctx.Response.StatusCode = 500;
                    ctx.Response.ContentType = Constants.JsonContentType;

                    resp = new ApiErrorResponse
                    {
                        Error = ApiResultEnum.InternalError
                    };

                    await ctx.Response.Send(_Serializer.SerializeJson(resp, true));

                    #endregion
                }
                else
                {
                    if (result.AuthenticationResult == AuthenticationResultEnum.Success 
                        && result.AuthorizationResult == AuthorizationResultEnum.Permitted)
                    {
                        #region Success

                        // do nothing

                        #endregion
                    }
                    else
                    {
                        _Logger.Warn(_LogHeader + "authentication or authorization failure");
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = Constants.JsonContentType;

                        resp = new ApiErrorResponse
                        {
                            Error = ApiResultEnum.NotAuthorized
                        };

                        await ctx.Response.Send(_Serializer.SerializeJson(resp, true));
                    }
                }
            }
            catch (Exception e)
            {
                _Logger.Warn(_LogHeader + "authentication exception:" + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;

                resp = new ApiErrorResponse
                {
                    Error = ApiResultEnum.InternalError,
                    Message = e.Message,
                    Data = e.Data
                };

                await ctx.Response.Send(_Serializer.SerializeJson(resp, true));
            }
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
