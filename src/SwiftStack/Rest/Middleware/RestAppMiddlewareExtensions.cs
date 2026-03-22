namespace SwiftStack.Rest.Middleware
{
    using System;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;

    /// <summary>
    /// Extension methods for RestApp to register middleware.
    /// </summary>
    public static class RestAppMiddlewareExtensions
    {
        /// <summary>
        /// Add a middleware delegate to the REST request pipeline.
        /// Middleware executes in registration order, wrapping the route handler.
        /// Call next() to continue the pipeline, or return without calling next() to short-circuit.
        /// Must be called before Run().
        /// </summary>
        /// <param name="app">The REST application.</param>
        /// <param name="middleware">The middleware delegate.</param>
        /// <returns>The REST application for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when app or middleware is null.</exception>
        public static RestApp Use(this RestApp app, RestMiddlewareDelegate middleware)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (middleware == null) throw new ArgumentNullException(nameof(middleware));

            app.Middleware.Add(middleware);
            return app;
        }

        /// <summary>
        /// Add a middleware delegate to the REST request pipeline (convenience overload without CancellationToken).
        /// Middleware executes in registration order, wrapping the route handler.
        /// Call next() to continue the pipeline, or return without calling next() to short-circuit.
        /// Must be called before Run().
        /// </summary>
        /// <param name="app">The REST application.</param>
        /// <param name="middleware">The middleware delegate.</param>
        /// <returns>The REST application for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when app or middleware is null.</exception>
        public static RestApp Use(this RestApp app, Func<HttpContextBase, Func<Task>, Task> middleware)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (middleware == null) throw new ArgumentNullException(nameof(middleware));

            app.Middleware.Add((HttpContextBase ctx, Func<Task> next, System.Threading.CancellationToken token) =>
            {
                return middleware(ctx, next);
            });

            return app;
        }
    }
}
