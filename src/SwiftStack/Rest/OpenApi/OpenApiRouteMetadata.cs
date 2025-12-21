namespace SwiftStack.Rest.OpenApi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAPI metadata for a single route/operation.
    /// Provides a fluent API for building route documentation.
    /// </summary>
    public class OpenApiRouteMetadata
    {
        #region Public-Members

        /// <summary>
        /// A list of tags for API documentation control.
        /// </summary>
        [JsonPropertyName("tags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> Tags { get; set; } = null;

        /// <summary>
        /// A short summary of what the operation does.
        /// </summary>
        [JsonPropertyName("summary")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Summary { get; set; } = null;

        /// <summary>
        /// A verbose explanation of the operation behavior.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// Additional external documentation for this operation.
        /// </summary>
        [JsonPropertyName("externalDocs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiExternalDocs ExternalDocs { get; set; } = null;

        /// <summary>
        /// Unique string used to identify the operation.
        /// </summary>
        [JsonPropertyName("operationId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string OperationId { get; set; } = null;

        /// <summary>
        /// A list of parameters that are applicable for this operation.
        /// </summary>
        [JsonPropertyName("parameters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<OpenApiParameterMetadata> Parameters { get; set; } = null;

        /// <summary>
        /// The request body applicable for this operation.
        /// </summary>
        [JsonPropertyName("requestBody")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiRequestBodyMetadata RequestBody { get; set; } = null;

        /// <summary>
        /// The possible responses as they are returned from executing this operation.
        /// The key is the HTTP status code as a string (e.g., "200", "404").
        /// </summary>
        [JsonPropertyName("responses")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, OpenApiResponseMetadata> Responses { get; set; } = null;

        /// <summary>
        /// A declaration of which security mechanisms can be used for this operation.
        /// </summary>
        [JsonPropertyName("security")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Dictionary<string, List<string>>> Security { get; set; } = null;

        /// <summary>
        /// Declares this operation to be deprecated.
        /// </summary>
        [JsonPropertyName("deprecated")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Deprecated { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty route metadata.
        /// </summary>
        public OpenApiRouteMetadata()
        {
        }

        /// <summary>
        /// Instantiates route metadata with summary and optional tag.
        /// </summary>
        /// <param name="summary">A short summary of what the operation does.</param>
        /// <param name="tag">A tag for API documentation control.</param>
        public OpenApiRouteMetadata(string summary, string tag = null)
        {
            Summary = summary;
            if (!string.IsNullOrEmpty(tag))
                Tags = new List<string> { tag };
        }

        /// <summary>
        /// Creates route metadata with summary and optional tag.
        /// </summary>
        /// <param name="summary">A short summary of what the operation does.</param>
        /// <param name="tag">A tag for API documentation control.</param>
        /// <returns>A new route metadata instance.</returns>
        public static OpenApiRouteMetadata Create(string summary, string tag = null)
        {
            return new OpenApiRouteMetadata(summary, tag);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Adds a tag to the operation.
        /// </summary>
        /// <param name="tag">The tag to add.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithTag(string tag)
        {
            if (Tags == null)
                Tags = new List<string>();
            Tags.Add(tag);
            return this;
        }

        /// <summary>
        /// Adds multiple tags to the operation.
        /// </summary>
        /// <param name="tags">The tags to add.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithTags(params string[] tags)
        {
            if (Tags == null)
                Tags = new List<string>();
            Tags.AddRange(tags);
            return this;
        }

        /// <summary>
        /// Sets the summary for the operation.
        /// </summary>
        /// <param name="summary">A short summary of what the operation does.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithSummary(string summary)
        {
            Summary = summary;
            return this;
        }

        /// <summary>
        /// Sets the description for the operation.
        /// </summary>
        /// <param name="description">A verbose explanation of the operation behavior.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithDescription(string description)
        {
            Description = description;
            return this;
        }

        /// <summary>
        /// Sets the operation ID.
        /// </summary>
        /// <param name="operationId">Unique string used to identify the operation.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithOperationId(string operationId)
        {
            OperationId = operationId;
            return this;
        }

        /// <summary>
        /// Adds a parameter to the operation.
        /// </summary>
        /// <param name="parameter">The parameter to add.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithParameter(OpenApiParameterMetadata parameter)
        {
            if (Parameters == null)
                Parameters = new List<OpenApiParameterMetadata>();
            Parameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// Adds multiple parameters to the operation.
        /// </summary>
        /// <param name="parameters">The parameters to add.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithParameters(params OpenApiParameterMetadata[] parameters)
        {
            if (Parameters == null)
                Parameters = new List<OpenApiParameterMetadata>();
            Parameters.AddRange(parameters);
            return this;
        }

        /// <summary>
        /// Sets the request body for the operation.
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithRequestBody(OpenApiRequestBodyMetadata requestBody)
        {
            RequestBody = requestBody;
            return this;
        }

        /// <summary>
        /// Adds a response to the operation.
        /// </summary>
        /// <param name="statusCode">The HTTP status code as an integer.</param>
        /// <param name="response">The response metadata.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithResponse(int statusCode, OpenApiResponseMetadata response)
        {
            if (Responses == null)
                Responses = new Dictionary<string, OpenApiResponseMetadata>();
            Responses[statusCode.ToString()] = response;
            return this;
        }

        /// <summary>
        /// Adds a successful (200) response to the operation.
        /// </summary>
        /// <param name="response">The response metadata.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithSuccessResponse(OpenApiResponseMetadata response)
        {
            return WithResponse(200, response);
        }

        /// <summary>
        /// Adds a security requirement to the operation.
        /// </summary>
        /// <param name="schemeName">The name of the security scheme.</param>
        /// <param name="scopes">Optional scopes for OAuth2.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithSecurity(string schemeName, params string[] scopes)
        {
            if (Security == null)
                Security = new List<Dictionary<string, List<string>>>();

            Dictionary<string, List<string>> requirement = new Dictionary<string, List<string>>
            {
                [schemeName] = new List<string>(scopes)
            };
            Security.Add(requirement);
            return this;
        }

        /// <summary>
        /// Marks the operation as deprecated.
        /// </summary>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata AsDeprecated()
        {
            Deprecated = true;
            return this;
        }

        /// <summary>
        /// Sets external documentation for the operation.
        /// </summary>
        /// <param name="url">The URL for the external documentation.</param>
        /// <param name="description">A description of the external documentation.</param>
        /// <returns>This instance for method chaining.</returns>
        public OpenApiRouteMetadata WithExternalDocs(string url, string description = null)
        {
            ExternalDocs = new OpenApiExternalDocs(url, description);
            return this;
        }

        #endregion
    }
}
