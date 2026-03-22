namespace SwiftStack.Rest.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;

    /// <summary>
    /// Manages an ordered list of middleware delegates and executes them as a pipeline.
    /// Middleware is registered before the server starts and executed per-request.
    /// </summary>
    public class MiddlewarePipeline
    {
        private List<RestMiddlewareDelegate> _Middlewares = new List<RestMiddlewareDelegate>();

        /// <summary>
        /// Add a middleware delegate to the end of the pipeline.
        /// Must be called before the server starts.
        /// </summary>
        /// <param name="middleware">The middleware delegate to add.</param>
        public void Add(RestMiddlewareDelegate middleware)
        {
            if (middleware == null) throw new ArgumentNullException(nameof(middleware));
            _Middlewares.Add(middleware);
        }

        /// <summary>
        /// True if the pipeline has one or more middleware registered.
        /// </summary>
        public bool HasMiddleware
        {
            get { return _Middlewares.Count > 0; }
        }

        /// <summary>
        /// Execute the middleware pipeline, terminating with the provided handler.
        /// When no middleware is registered, the terminal handler is called directly.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="terminalHandler">The route handler to invoke at the end of the pipeline.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Execute(HttpContextBase context, Func<Task> terminalHandler, CancellationToken token)
        {
            if (_Middlewares.Count == 0)
            {
                await terminalHandler().ConfigureAwait(false);
                return;
            }

            int index = -1;

            Func<Task> next = null;
            next = () =>
            {
                index++;
                if (index < _Middlewares.Count)
                {
                    return _Middlewares[index](context, next, token);
                }
                else
                {
                    return terminalHandler();
                }
            };

            await next().ConfigureAwait(false);
        }
    }
}
