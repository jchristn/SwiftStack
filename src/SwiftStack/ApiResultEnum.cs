namespace SwiftStack
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// API result.
    /// </summary>
    public enum ApiResultEnum
    {
        /// <summary>
        /// Success.
        /// </summary>
        Success,
        /// <summary>
        /// NotFound.
        /// </summary>
        NotFound,
        /// <summary>
        /// Created.
        /// </summary>
        Created,
        /// <summary>
        /// NotAuthorized.
        /// </summary>
        NotAuthorized,
        /// <summary>
        /// InternalError.
        /// </summary>
        InternalError,
        /// <summary>
        /// SlowDown.
        /// </summary>
        SlowDown,
        /// <summary>
        /// Conflict.
        /// </summary>
        Conflict,
        /// <summary>
        /// BadRequest.
        /// </summary>
        BadRequest,
        /// <summary>
        /// DeserializationError.
        /// </summary>
        DeserializationError
    }
}
