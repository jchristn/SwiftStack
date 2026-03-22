namespace SwiftStack.Rest.Health
{
    /// <summary>
    /// Health status of the application.
    /// </summary>
    public enum HealthStatusEnum
    {
        /// <summary>
        /// The application is healthy and operating normally.
        /// </summary>
        Healthy,

        /// <summary>
        /// The application is operational but experiencing degraded performance or partial failures.
        /// </summary>
        Degraded,

        /// <summary>
        /// The application is unhealthy and unable to serve requests properly.
        /// </summary>
        Unhealthy
    }
}
