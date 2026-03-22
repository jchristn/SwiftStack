namespace SwiftStack.Rest
{
    using System;

    /// <summary>
    /// Extension methods for RestApp to configure request timeouts.
    /// </summary>
    public static class RestAppTimeoutExtensions
    {
        /// <summary>
        /// Enable request timeouts with the specified default timeout duration.
        /// Requests that exceed the timeout will receive a 408 Request Timeout response.
        /// The cancellation token is available to route handlers via AppRequest.CancellationToken.
        /// </summary>
        /// <param name="app">The REST application.</param>
        /// <param name="defaultTimeout">Default timeout for all requests.</param>
        /// <returns>The REST application for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when app is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is negative.</exception>
        public static RestApp UseTimeout(this RestApp app, TimeSpan defaultTimeout)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            RestTimeoutSettings settings = new RestTimeoutSettings
            {
                DefaultTimeout = defaultTimeout
            };

            app.TimeoutSettings = settings;
            return app;
        }

        /// <summary>
        /// Enable request timeouts with configurable settings.
        /// Requests that exceed the timeout will receive a 408 Request Timeout response.
        /// The cancellation token is available to route handlers via AppRequest.CancellationToken.
        /// </summary>
        /// <param name="app">The REST application.</param>
        /// <param name="configure">Action to configure timeout settings.</param>
        /// <returns>The REST application for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when app or configure is null.</exception>
        public static RestApp UseTimeout(this RestApp app, Action<RestTimeoutSettings> configure)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            RestTimeoutSettings settings = new RestTimeoutSettings();
            configure(settings);

            app.TimeoutSettings = settings;
            return app;
        }
    }
}
