namespace SwiftStack.Rest.OpenApi
{
    /// <summary>
    /// Specifies the location of a parameter in an HTTP request.
    /// </summary>
    public enum ParameterLocation
    {
        /// <summary>
        /// Parameter is part of the URL path (e.g., /users/{id}).
        /// </summary>
        Path,

        /// <summary>
        /// Parameter is in the query string (e.g., ?page=1).
        /// </summary>
        Query,

        /// <summary>
        /// Parameter is in the request headers.
        /// </summary>
        Header,

        /// <summary>
        /// Parameter is in a cookie.
        /// </summary>
        Cookie
    }
}
