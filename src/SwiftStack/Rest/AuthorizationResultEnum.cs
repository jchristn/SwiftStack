namespace SwiftStack.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Authorization result.
    /// </summary>
    public enum AuthorizationResultEnum
    {
        /// <summary>
        /// Permitted.
        /// </summary>
        Permitted,
        /// <summary>
        /// DeniedExplicit.
        /// </summary>
        DeniedExplicit,
        /// <summary>
        /// DeniedImplicit.
        /// </summary>
        DeniedImplicit,
        /// <summary>
        /// NotFound.
        /// </summary>
        NotFound,
        /// <summary>
        /// Conflict.
        /// </summary>
        Conflict
    }
}
