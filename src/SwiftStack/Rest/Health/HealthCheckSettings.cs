namespace SwiftStack.Rest.Health
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Settings for the health check endpoint.
    /// </summary>
    public class HealthCheckSettings
    {
        /// <summary>
        /// Path for the health check endpoint.
        /// Default is /health.
        /// Must start with /.
        /// </summary>
        public string Path
        {
            get { return _Path; }
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Path));
                if (!value.StartsWith("/")) throw new ArgumentException("Path must start with /.");
                _Path = value;
            }
        }

        /// <summary>
        /// Whether the health check endpoint requires authentication.
        /// Default is false.
        /// </summary>
        public bool RequireAuthentication { get; set; } = false;

        /// <summary>
        /// Optional custom health check delegate.
        /// When null, the default returns a healthy status.
        /// </summary>
        public Func<CancellationToken, Task<HealthCheckResult>> CustomCheck { get; set; } = null;

        private string _Path = "/health";
    }
}
