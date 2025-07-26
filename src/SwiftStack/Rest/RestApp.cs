namespace SwiftStack.Rest
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
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// SwiftStack REST application.
    /// </summary>
    public class RestApp : IDisposable
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
        /// Preflight route.
        /// </summary>
        public Func<HttpContextBase, Task> PreflightRoute { get; set; } = null;

        /// <summary>
        /// Authentication route.
        /// </summary>
        public Func<HttpContextBase, Task<AuthResult>> AuthenticationRoute { get; set; } = null;

        /// <summary>
        /// Favicon.ico file.
        /// </summary>
        public string FaviconFile { get; set; } = "./assets/favicon.ico";

        #endregion

        #region Private-Members

        private SwiftStackApp _App = null;

        private string _Header = "[RestApp] ";
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
        /// SwiftStack REST application.
        /// </summary>
        /// <param name="app">SwiftStack app.</param>
        public RestApp(SwiftStackApp app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            _App = app;
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
                    using (_Webserver = new Webserver(_WebserverSettings, DefaultRoute))
                    {
                        _App.Logging.Debug(_Header + "starting webserver on " + _WebserverSettings.Prefix);

                        if (!string.IsNullOrEmpty(FaviconFile))
                        {
                            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/favicon.ico", FaviconReadRoute);
                            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.HEAD, "/favicon.ico", FaviconExistsRoute);
                        }

                        if (_UnauthenticatedRoutes != null && _UnauthenticatedRoutes.Count > 0)
                        {
                            string unauthenticatedRoutes = "";
                            int unauthMaxMethodLength = _UnauthenticatedRoutes.Max(s => s.Method.ToString().Length);

                            for (int i = 0; i < _UnauthenticatedRoutes.Count; i++)
                            {
                                _Webserver.Routes.PreAuthentication.Parameter.Add(
                                    _UnauthenticatedRoutes[i].Method,
                                    _UnauthenticatedRoutes[i].Path,
                                    _UnauthenticatedRoutes[i].Handler,
                                    ExceptionRoute);

                                if (i > 0) unauthenticatedRoutes += Environment.NewLine;
                                unauthenticatedRoutes +=
                                    "| [" + _UnauthenticatedRoutes[i].Method.ToString().PadRight(unauthMaxMethodLength) + "] " + _UnauthenticatedRoutes[i].Path;
                            }

                            if (!string.IsNullOrEmpty(unauthenticatedRoutes) && !QuietStartup)
                                _App.Logging.Debug(_Header + "initialized unauthenticated routes:" + Environment.NewLine + unauthenticatedRoutes);
                        }

                        if (_AuthenticatedRoutes != null && _AuthenticatedRoutes.Count > 0)
                        {
                            string authenticatedRoutes = "";
                            int authMaxMethodLength = _AuthenticatedRoutes.Max(s => s.Method.ToString().Length);

                            for (int i = 0; i < _AuthenticatedRoutes.Count; i++)
                            {
                                _Webserver.Routes.PostAuthentication.Parameter.Add(
                                    _AuthenticatedRoutes[i].Method,
                                    _AuthenticatedRoutes[i].Path,
                                    _AuthenticatedRoutes[i].Handler,
                                    ExceptionRoute);

                                if (i > 0) authenticatedRoutes += Environment.NewLine;
                                authenticatedRoutes +=
                                    "| [" + _AuthenticatedRoutes[i].Method.ToString().PadRight(authMaxMethodLength) + "] " + _AuthenticatedRoutes[i].Path;
                            }

                            if (!string.IsNullOrEmpty(authenticatedRoutes) && !QuietStartup)
                                _App.Logging.Debug(_Header + "initialized authenticated routes:" + Environment.NewLine + authenticatedRoutes);
                        }

                        _Webserver.Routes.Preflight = PreflightRoute != null ? PreflightRoute : PreflightInternalRoute;
                        _Webserver.Routes.PreRouting = PreRoutingRoute;
                        _Webserver.Routes.PostRouting = PostRoutingRoute;
                        if (AuthenticationRoute != null) _Webserver.Routes.AuthenticateRequest = AuthenticateRequest;

                        await _Webserver.StartAsync(token);
                    }
                }
                catch (Exception e)
                {
                    _App.Logging.Alert(
                        "exception starting webserver on " + _WebserverSettings.Prefix + Environment.NewLine +
                        e.ToString());
                }
            });

            IsRunning = true;

            while (true)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (token.IsCancellationRequested) break;
            }

            IsRunning = false;

            _App.Logging.Info(_Header + "REST application stopped");
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
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
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
                    if (!string.IsNullOrEmpty(ctx.Request.DataAsString))
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
                                requestObj = _App.Serializer.DeserializeJson<object>(ctx.Request.DataAsString);
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

                    AppRequest dynamicReq = new AppRequest(ctx, _App.Serializer, requestObj);
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
            _App.Logging.Debug(_Header + "added route " + method + " " + path);
        }

        #endregion

        #region Private-Methods

        private void RegisterRoute(
            HttpMethod method,
            string path,
            Func<HttpContextBase, Task> handler,
            bool requireAuthentication)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!path.StartsWith("/")) throw new ArgumentException("Paths must start with /.");
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            // Add route to appropriate collection
            List<ParameterRoute> routes = requireAuthentication ? _AuthenticatedRoutes : _UnauthenticatedRoutes;
            routes.Add(new ParameterRoute(method, path, handler, ExceptionRoute));
            _App.Logging.Debug(_Header + "added route " + method + " " + path);
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
                    AppRequest dynamicReq = new AppRequest(ctx, _App.Serializer, null);
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
                    if (!string.IsNullOrEmpty(ctx.Request.DataAsString))
                    {
                        // Handle string type differently
                        if (typeof(T) == typeof(string))
                        {
                            requestData = (T)(object)ctx.Request.DataAsString;
                        }
                        // Handle primitive types
                        else if (typeof(T).IsPrimitive)
                        {
                            requestData = (T)Convert.ChangeType(ctx.Request.DataAsString, typeof(T));
                        }
                        // For complex objects, use JSON deserialization
                        else
                        {
                            requestData = _App.Serializer.DeserializeJson<T>(ctx.Request.DataAsString);
                        }
                    }

                    AppRequest dynamicReq = new AppRequest(ctx, _App.Serializer, requestData);
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
            _App.Logging.Warn(_Header + "invalid method or URL in request from " + requestor + ": " + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery);
            ctx.Response.StatusCode = 400;
            await ctx.Response.Send(_App.Serializer.SerializeJson(new ApiErrorResponse
            {
                Error = ApiResultEnum.BadRequest,
            }, true));
        }

        private async Task PreflightInternalRoute(HttpContextBase ctx)
        {
            NameValueCollection nameValueCollection = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
            string[] array = null;
            string text = "";
            if (ctx.Request.Headers != null)
            {
                for (int i = 0; i < ctx.Request.Headers.Count; i++)
                {
                    string key = ctx.Request.Headers.GetKey(i);
                    string text2 = ctx.Request.Headers.Get(i);
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(text2) && string.Compare(key.ToLower(), "access-control-request-headers") == 0)
                    {
                        array = text2.Split(',');
                        break;
                    }
                }
            }

            if (array != null)
            {
                string[] array2 = array;
                foreach (string text3 in array2)
                {
                    text = text + ", " + text3;
                }
            }

            nameValueCollection.Add("Access-Control-Allow-Methods", "OPTIONS, HEAD, GET, PUT, POST, DELETE");
            nameValueCollection.Add("Access-Control-Allow-Headers", "*, Content-Type, X-Requested-With, " + text);
            nameValueCollection.Add("Access-Control-Expose-Headers", "Content-Type, X-Requested-With, " + text);
            nameValueCollection.Add("Access-Control-Allow-Origin", "*");
            nameValueCollection.Add("Accept", "*/*");
            nameValueCollection.Add("Accept-Language", "en-US, en");
            nameValueCollection.Add("Accept-Charset", "ISO-8859-1, utf-8");
            nameValueCollection.Add("Connection", "keep-alive");
            ctx.Response.StatusCode = 200;
            ctx.Response.Headers = nameValueCollection;
            await ctx.Response.Send();
        }

        private async Task PreRoutingRoute(HttpContextBase ctx)
        {

        }

        private async Task PostRoutingRoute(HttpContextBase ctx)
        {
            ctx.Request.Timestamp.End = DateTime.UtcNow;

            _App.Logging.Debug(_Header +
                ctx.Request.Method + " " +
                ctx.Request.Url.RawWithQuery + ": " +
                ctx.Response.StatusCode + " " +
                "(" + ctx.Request.Timestamp.TotalMs.Value.ToString("0.00") + "ms)");
        }

        private async Task DefaultExceptionRoute(HttpContextBase ctx, Exception e)
        {
            if (e is SwiftStackException swiftEx)
            {
                ApiErrorResponse error = new ApiErrorResponse
                {
                    Error = swiftEx.Result,
                    Data = swiftEx.Data
                };

                ctx.Response.StatusCode = swiftEx.StatusCode;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_App.Serializer.SerializeJson(error, true));
                _App.Logging.Warn(_Header + "SwiftStack exception: " + swiftEx.Message);
            }
            else if (e is JsonException jsonEx)
            {
                ApiErrorResponse error = new ApiErrorResponse
                {
                    Error = ApiResultEnum.DeserializationError,
                    Message = jsonEx.Message,
                    Data = jsonEx.Data
                };

                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_App.Serializer.SerializeJson(error, true));
                _App.Logging.Warn(_Header + "JSON exception: " + Environment.NewLine + jsonEx.ToString());
            }
            else
            {
                ApiErrorResponse error = new ApiErrorResponse
                {
                    Error = ApiResultEnum.InternalError,
                    Message = e.Message,
                    Data = e.Data
                };

                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_App.Serializer.SerializeJson(error, true));
                _App.Logging.Warn(_Header + "exception: " + Environment.NewLine + e.ToString());
            }
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

                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send(_App.Serializer.SerializeJson(error, true));
                }
            }
            else if (e is SwiftStackException swiftEx)
            {
                ApiErrorResponse error = new ApiErrorResponse
                {
                    Error = swiftEx.Result,
                    Data = swiftEx.Data
                };

                ctx.Response.StatusCode = swiftEx.StatusCode;
                await ctx.Response.Send(_App.Serializer.SerializeJson(error, true));
            }
            else if (e is JsonException jsonEx)
            {
                ApiErrorResponse error = new ApiErrorResponse
                {
                    Error = ApiResultEnum.DeserializationError,
                    Message = jsonEx.Message,
                    Data = jsonEx.Data
                };

                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_App.Serializer.SerializeJson(error, true));
            }
            else
            {
                ApiErrorResponse error = new ApiErrorResponse
                {
                    Error = ApiResultEnum.InternalError,
                    Message = e.Message,
                    Data = e.Data
                };

                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_App.Serializer.SerializeJson(error, true));
            }
        }

        private async Task ProcessResult(HttpContextBase ctx, object result)
        {
            try
            {
                if (!ctx.Response.ServerSentEvents && !ctx.Response.ChunkedTransfer)
                {
                    if (result == null)
                    {
                        await ctx.Response.Send();
                        return;
                    }

                    if (result is string stringResult)
                    {
                        if (String.IsNullOrEmpty(ctx.Response.ContentType))
                            ctx.Response.Headers.Add("Content-Type", "text/plain");
                        await ctx.Response.Send(stringResult);
                        return;
                    }

                    if (result != null && result.GetType().IsPrimitive)
                    {
                        if (String.IsNullOrEmpty(ctx.Response.ContentType))
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
                                if (String.IsNullOrEmpty(ctx.Response.ContentType))
                                    ctx.Response.Headers.Add("Content-Type", "application/json");
                                await ctx.Response.Send(_App.Serializer.SerializeJson(item1));
                            }
                            else
                            {
                                await ctx.Response.Send();
                            }

                            return;
                        }
                    }

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
                                    if (String.IsNullOrEmpty(ctx.Response.ContentType))
                                        ctx.Response.Headers.Add("Content-Type", "text/plain");
                                    await ctx.Response.Send(itemString);
                                }
                                else if (item1.GetType().IsPrimitive)
                                {
                                    if (String.IsNullOrEmpty(ctx.Response.ContentType))
                                        ctx.Response.Headers.Add("Content-Type", "text/plain");
                                    await ctx.Response.Send(item1.ToString());
                                }
                                else
                                {
                                    if (String.IsNullOrEmpty(ctx.Response.ContentType))
                                        ctx.Response.Headers.Add("Content-Type", "application/json");
                                    await ctx.Response.Send(_App.Serializer.SerializeJson(item1));
                                }
                            }
                            else
                            {
                                await ctx.Response.Send();
                            }

                            return;
                        }
                    }

                    ctx.Response.Headers.Add("Content-Type", "application/json");
                    await ctx.Response.Send(_App.Serializer.SerializeJson(result));
                }
                else
                {
                    // SSE and chunked transfer encoding responses are managed within the route implementation
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

                    _App.Logging.Warn(_Header + "no response from authentication route");
                    ctx.Response.StatusCode = 500;
                    ctx.Response.ContentType = Constants.JsonContentType;

                    resp = new ApiErrorResponse
                    {
                        Error = ApiResultEnum.InternalError
                    };

                    await ctx.Response.Send(_App.Serializer.SerializeJson(resp, true));

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
                        _App.Logging.Warn(_Header + "authentication or authorization failure");
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = Constants.JsonContentType;

                        resp = new ApiErrorResponse
                        {
                            Error = ApiResultEnum.NotAuthorized
                        };

                        await ctx.Response.Send(_App.Serializer.SerializeJson(resp, true));
                    }
                }
            }
            catch (Exception e)
            {
                _App.Logging.Warn(_Header + "authentication exception:" + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;

                resp = new ApiErrorResponse
                {
                    Error = ApiResultEnum.InternalError,
                    Message = e.Message,
                    Data = e.Data
                };

                await ctx.Response.Send(_App.Serializer.SerializeJson(resp, true));
            }
        }

        private async Task FaviconReadRoute(HttpContextBase ctx)
        {
            if (File.Exists(FaviconFile))
            {
                try
                {
                    ctx.Response.ContentType = "image/x-icon";
                    byte[] bytes = File.ReadAllBytes(FaviconFile);
                    await ctx.Response.Send(bytes, _Token).ConfigureAwait(false);
                    return;
                }
                catch (Exception e)
                {
                    _App.Logging.Warn(_Header + "exception reading favicon file " + FaviconFile + Environment.NewLine + e.ToString());
                }
            }

            ctx.Response.StatusCode = 404;
            await ctx.Response.Send();
        }

        private async Task FaviconExistsRoute(HttpContextBase ctx)
        {
            if (File.Exists(FaviconFile))
            {
                try
                {
                    ctx.Response.ContentType = "image/x-icon";
                    await ctx.Response.Send(new FileInfo(FaviconFile).Length, _Token).ConfigureAwait(false);
                    return;
                }
                catch (Exception e)
                {
                    _App.Logging.Warn(_Header + "exception reading favicon file " + FaviconFile + Environment.NewLine + e.ToString());
                }
            }

            ctx.Response.StatusCode = 404;
            await ctx.Response.Send();
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
