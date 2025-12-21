namespace SwiftStack.Rest.OpenApi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines a security scheme that can be used by the operations.
    /// </summary>
    public class OpenApiSecurityScheme
    {
        #region Public-Members

        /// <summary>
        /// The type of the security scheme.
        /// Valid values are "apiKey", "http", "mutualTLS", "oauth2", "openIdConnect".
        /// Required.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "http";

        /// <summary>
        /// A short description for the security scheme.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// The name of the header, query or cookie parameter to be used.
        /// Required for apiKey type.
        /// </summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; } = null;

        /// <summary>
        /// The location of the API key.
        /// Valid values are "query", "header", or "cookie".
        /// Required for apiKey type.
        /// </summary>
        [JsonPropertyName("in")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string In { get; set; } = null;

        /// <summary>
        /// The name of the HTTP Authorization scheme to be used in the Authorization header.
        /// Required for http type.
        /// </summary>
        [JsonPropertyName("scheme")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Scheme { get; set; } = null;

        /// <summary>
        /// A hint to the client to identify how the bearer token is formatted.
        /// </summary>
        [JsonPropertyName("bearerFormat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string BearerFormat { get; set; } = null;

        /// <summary>
        /// An object containing configuration information for the flow types supported.
        /// Required for oauth2 type.
        /// </summary>
        [JsonPropertyName("flows")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiOAuthFlows Flows { get; set; } = null;

        /// <summary>
        /// OpenId Connect URL to discover OAuth2 configuration values.
        /// Required for openIdConnect type.
        /// </summary>
        [JsonPropertyName("openIdConnectUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string OpenIdConnectUrl { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty security scheme.
        /// </summary>
        public OpenApiSecurityScheme()
        {
        }

        /// <summary>
        /// Creates an API Key security scheme.
        /// </summary>
        /// <param name="name">The name of the header, query or cookie parameter.</param>
        /// <param name="location">The location of the API key ("query", "header", or "cookie").</param>
        /// <param name="description">A short description for the security scheme.</param>
        /// <returns>A configured API Key security scheme.</returns>
        public static OpenApiSecurityScheme ApiKey(string name, string location = "header", string description = null)
        {
            return new OpenApiSecurityScheme
            {
                Type = "apiKey",
                Name = name,
                In = location,
                Description = description
            };
        }

        /// <summary>
        /// Creates a Bearer token security scheme.
        /// </summary>
        /// <param name="bearerFormat">A hint to the client to identify how the bearer token is formatted (e.g., "JWT").</param>
        /// <param name="description">A short description for the security scheme.</param>
        /// <returns>A configured Bearer security scheme.</returns>
        public static OpenApiSecurityScheme Bearer(string bearerFormat = null, string description = null)
        {
            return new OpenApiSecurityScheme
            {
                Type = "http",
                Scheme = "bearer",
                BearerFormat = bearerFormat,
                Description = description
            };
        }

        /// <summary>
        /// Creates a Basic authentication security scheme.
        /// </summary>
        /// <param name="description">A short description for the security scheme.</param>
        /// <returns>A configured Basic auth security scheme.</returns>
        public static OpenApiSecurityScheme Basic(string description = null)
        {
            return new OpenApiSecurityScheme
            {
                Type = "http",
                Scheme = "basic",
                Description = description
            };
        }

        #endregion
    }

    /// <summary>
    /// Allows configuration of the supported OAuth Flows.
    /// </summary>
    public class OpenApiOAuthFlows
    {
        #region Public-Members

        /// <summary>
        /// Configuration for the OAuth Implicit flow.
        /// </summary>
        [JsonPropertyName("implicit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiOAuthFlow Implicit { get; set; } = null;

        /// <summary>
        /// Configuration for the OAuth Resource Owner Password flow.
        /// </summary>
        [JsonPropertyName("password")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiOAuthFlow Password { get; set; } = null;

        /// <summary>
        /// Configuration for the OAuth Client Credentials flow.
        /// </summary>
        [JsonPropertyName("clientCredentials")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiOAuthFlow ClientCredentials { get; set; } = null;

        /// <summary>
        /// Configuration for the OAuth Authorization Code flow.
        /// </summary>
        [JsonPropertyName("authorizationCode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiOAuthFlow AuthorizationCode { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates empty OAuth flows.
        /// </summary>
        public OpenApiOAuthFlows()
        {
        }

        #endregion
    }

    /// <summary>
    /// Configuration details for a supported OAuth Flow.
    /// </summary>
    public class OpenApiOAuthFlow
    {
        #region Public-Members

        /// <summary>
        /// The authorization URL to be used for this flow.
        /// Required for implicit and authorizationCode flows.
        /// </summary>
        [JsonPropertyName("authorizationUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string AuthorizationUrl { get; set; } = null;

        /// <summary>
        /// The token URL to be used for this flow.
        /// Required for password, clientCredentials and authorizationCode flows.
        /// </summary>
        [JsonPropertyName("tokenUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TokenUrl { get; set; } = null;

        /// <summary>
        /// The URL to be used for obtaining refresh tokens.
        /// </summary>
        [JsonPropertyName("refreshUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string RefreshUrl { get; set; } = null;

        /// <summary>
        /// The available scopes for the OAuth2 security scheme.
        /// Required.
        /// </summary>
        [JsonPropertyName("scopes")]
        public Dictionary<string, string> Scopes { get; set; } = new Dictionary<string, string>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty OAuth flow.
        /// </summary>
        public OpenApiOAuthFlow()
        {
        }

        #endregion
    }
}
