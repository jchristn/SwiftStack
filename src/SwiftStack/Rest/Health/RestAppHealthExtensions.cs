namespace SwiftStack.Rest.Health
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for RestApp to enable health check endpoints.
    /// </summary>
    public static class RestAppHealthExtensions
    {
        /// <summary>
        /// Enables a health check endpoint for the REST application.
        /// When no custom check is configured, returns a healthy status with HTTP 200.
        /// Custom checks can return Degraded (HTTP 200) or Unhealthy (HTTP 503).
        /// </summary>
        /// <param name="app">The REST application.</param>
        /// <param name="configure">Optional action to configure health check settings.</param>
        /// <returns>The REST application for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when app is null.</exception>
        public static RestApp UseHealthCheck(this RestApp app, Action<HealthCheckSettings> configure = null)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            HealthCheckSettings settings = new HealthCheckSettings();
            configure?.Invoke(settings);

            app.Get(settings.Path, async (AppRequest req) =>
            {
                HealthCheckResult result;

                if (settings.CustomCheck != null)
                {
                    result = await settings.CustomCheck(CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    result = new HealthCheckResult
                    {
                        Status = HealthStatusEnum.Healthy
                    };
                }

                if (result.Status == HealthStatusEnum.Unhealthy)
                {
                    req.Http.Response.StatusCode = 503;
                }

                return result;
            },
            requireAuthentication: settings.RequireAuthentication);

            return app;
        }
    }
}
