namespace SwiftStack.Rest.Health
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Result of a health check.
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Health status.
        /// </summary>
        public HealthStatusEnum Status { get; set; } = HealthStatusEnum.Healthy;

        /// <summary>
        /// Optional description of the health status.
        /// </summary>
        public string Description { get; set; } = null;

        /// <summary>
        /// Optional key-value metadata, such as uptime, version, or dependency statuses.
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = null;
    }
}
