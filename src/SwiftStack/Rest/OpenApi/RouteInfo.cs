namespace SwiftStack.Rest.OpenApi
{
    using System;
    using WatsonWebserver.Core;

    /// <summary>
    /// Extended route information including OpenAPI metadata.
    /// Used internally to track route registration for OpenAPI document generation.
    /// </summary>
    public class RouteInfo
    {
        #region Public-Members

        /// <summary>
        /// HTTP method for the route.
        /// </summary>
        public HttpMethod Method { get; set; } = HttpMethod.GET;

        /// <summary>
        /// URL path pattern (e.g., "/users/{id}").
        /// </summary>
        public string Path { get; set; } = null;

        /// <summary>
        /// Request body type for routes that accept a body (POST, PUT, PATCH, DELETE).
        /// Null for routes without a request body.
        /// </summary>
        public Type RequestBodyType { get; set; } = null;

        /// <summary>
        /// Response type for the route.
        /// Null if not explicitly specified.
        /// </summary>
        public Type ResponseType { get; set; } = null;

        /// <summary>
        /// OpenAPI documentation metadata for the route.
        /// May be null if no explicit documentation was provided.
        /// </summary>
        public OpenApiRouteMetadata OpenApiMetadata { get; set; } = null;

        /// <summary>
        /// Whether the route requires authentication.
        /// </summary>
        public bool RequiresAuthentication { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty route info.
        /// </summary>
        public RouteInfo()
        {
        }

        /// <summary>
        /// Instantiates route info with the specified values.
        /// </summary>
        /// <param name="method">HTTP method for the route.</param>
        /// <param name="path">URL path pattern.</param>
        /// <param name="requiresAuthentication">Whether the route requires authentication.</param>
        /// <param name="requestBodyType">Request body type.</param>
        /// <param name="responseType">Response type.</param>
        /// <param name="openApiMetadata">OpenAPI documentation metadata.</param>
        public RouteInfo(
            HttpMethod method,
            string path,
            bool requiresAuthentication = false,
            Type requestBodyType = null,
            Type responseType = null,
            OpenApiRouteMetadata openApiMetadata = null)
        {
            Method = method;
            Path = path;
            RequiresAuthentication = requiresAuthentication;
            RequestBodyType = requestBodyType;
            ResponseType = responseType;
            OpenApiMetadata = openApiMetadata;
        }

        #endregion
    }
}
