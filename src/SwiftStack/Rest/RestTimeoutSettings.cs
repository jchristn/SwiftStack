namespace SwiftStack.Rest
{
    using System;

    /// <summary>
    /// Settings for request timeout behavior.
    /// </summary>
    public class RestTimeoutSettings
    {
        /// <summary>
        /// Default timeout for all requests.
        /// When set to TimeSpan.Zero (the default), no timeout is applied.
        /// Must be zero or a positive value.
        /// </summary>
        public TimeSpan DefaultTimeout
        {
            get { return _DefaultTimeout; }
            set
            {
                if (value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(DefaultTimeout), "Timeout must be zero or positive.");
                _DefaultTimeout = value;
            }
        }

        private TimeSpan _DefaultTimeout = TimeSpan.Zero;
    }
}
