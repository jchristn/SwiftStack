namespace SwiftStack.Rest.OpenApi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Describes a single response from an API Operation.
    /// </summary>
    public class OpenApiResponseMetadata
    {
        #region Public-Members

        /// <summary>
        /// A short description of the response.
        /// Required.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "Successful response";

        /// <summary>
        /// Maps a header name to its definition.
        /// </summary>
        [JsonPropertyName("headers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, OpenApiHeaderMetadata> Headers { get; set; } = null;

        /// <summary>
        /// A map containing descriptions of potential response payloads.
        /// The key is a media type (e.g., "application/json").
        /// </summary>
        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, OpenApiMediaType> Content { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty response.
        /// </summary>
        public OpenApiResponseMetadata()
        {
        }

        /// <summary>
        /// Instantiates a response with the specified description.
        /// </summary>
        /// <param name="description">A short description of the response.</param>
        public OpenApiResponseMetadata(string description)
        {
            Description = description;
        }

        /// <summary>
        /// Creates a response with no content.
        /// </summary>
        /// <param name="description">A short description of the response.</param>
        /// <returns>A response with no content.</returns>
        public static OpenApiResponseMetadata Create(string description)
        {
            return new OpenApiResponseMetadata(description);
        }

        /// <summary>
        /// Creates a JSON response.
        /// </summary>
        /// <param name="description">A short description of the response.</param>
        /// <param name="schema">The schema for the response body.</param>
        /// <returns>A JSON response.</returns>
        public static OpenApiResponseMetadata Json(string description, OpenApiSchemaMetadata schema)
        {
            return new OpenApiResponseMetadata
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType(schema)
                }
            };
        }

        /// <summary>
        /// Creates a JSON response from a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to create a schema for.</typeparam>
        /// <param name="description">A short description of the response.</param>
        /// <returns>A JSON response with schema derived from the type.</returns>
        public static OpenApiResponseMetadata Json<T>(string description)
        {
            return Json(description, OpenApiSchemaMetadata.FromType<T>());
        }

        /// <summary>
        /// Creates a plain text response.
        /// </summary>
        /// <param name="description">A short description of the response.</param>
        /// <returns>A plain text response.</returns>
        public static OpenApiResponseMetadata Text(string description)
        {
            return new OpenApiResponseMetadata
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["text/plain"] = new OpenApiMediaType(OpenApiSchemaMetadata.String())
                }
            };
        }

        /// <summary>
        /// Creates a 200 OK response with JSON content.
        /// </summary>
        /// <param name="schema">The schema for the response body.</param>
        /// <returns>A 200 OK JSON response.</returns>
        public static OpenApiResponseMetadata Ok(OpenApiSchemaMetadata schema)
        {
            return Json("Successful operation", schema);
        }

        /// <summary>
        /// Creates a 201 Created response.
        /// </summary>
        /// <param name="schema">The schema for the response body.</param>
        /// <returns>A 201 Created response.</returns>
        public static OpenApiResponseMetadata Created(OpenApiSchemaMetadata schema = null)
        {
            OpenApiResponseMetadata response = new OpenApiResponseMetadata("Resource created successfully");
            if (schema != null)
            {
                response.Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType(schema)
                };
            }
            return response;
        }

        /// <summary>
        /// Creates a 204 No Content response.
        /// </summary>
        /// <returns>A 204 No Content response.</returns>
        public static OpenApiResponseMetadata NoContent()
        {
            return new OpenApiResponseMetadata("No content");
        }

        /// <summary>
        /// Creates a 400 Bad Request response.
        /// </summary>
        /// <param name="description">A short description of the error.</param>
        /// <returns>A 400 Bad Request response.</returns>
        public static OpenApiResponseMetadata BadRequest(string description = "Bad request")
        {
            return new OpenApiResponseMetadata(description);
        }

        /// <summary>
        /// Creates a 401 Unauthorized response.
        /// </summary>
        /// <param name="description">A short description of the error.</param>
        /// <returns>A 401 Unauthorized response.</returns>
        public static OpenApiResponseMetadata Unauthorized(string description = "Authentication required")
        {
            return new OpenApiResponseMetadata(description);
        }

        /// <summary>
        /// Creates a 403 Forbidden response.
        /// </summary>
        /// <param name="description">A short description of the error.</param>
        /// <returns>A 403 Forbidden response.</returns>
        public static OpenApiResponseMetadata Forbidden(string description = "Access denied")
        {
            return new OpenApiResponseMetadata(description);
        }

        /// <summary>
        /// Creates a 404 Not Found response.
        /// </summary>
        /// <param name="description">A short description of the error.</param>
        /// <returns>A 404 Not Found response.</returns>
        public static OpenApiResponseMetadata NotFound(string description = "Resource not found")
        {
            return new OpenApiResponseMetadata(description);
        }

        /// <summary>
        /// Creates a 409 Conflict response.
        /// </summary>
        /// <param name="description">A short description of the error.</param>
        /// <returns>A 409 Conflict response.</returns>
        public static OpenApiResponseMetadata Conflict(string description = "Resource conflict")
        {
            return new OpenApiResponseMetadata(description);
        }

        /// <summary>
        /// Creates a 429 Too Many Requests response.
        /// </summary>
        /// <param name="description">A short description of the error.</param>
        /// <returns>A 429 Too Many Requests response.</returns>
        public static OpenApiResponseMetadata TooManyRequests(string description = "Rate limit exceeded")
        {
            return new OpenApiResponseMetadata(description);
        }

        /// <summary>
        /// Creates a 500 Internal Server Error response.
        /// </summary>
        /// <param name="description">A short description of the error.</param>
        /// <returns>A 500 Internal Server Error response.</returns>
        public static OpenApiResponseMetadata InternalServerError(string description = "Internal server error")
        {
            return new OpenApiResponseMetadata(description);
        }

        #endregion
    }

    /// <summary>
    /// Describes a header included in a response.
    /// </summary>
    public class OpenApiHeaderMetadata
    {
        #region Public-Members

        /// <summary>
        /// A brief description of the header.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// Whether the header is required.
        /// </summary>
        [JsonPropertyName("required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Required { get; set; } = false;

        /// <summary>
        /// Whether the header is deprecated.
        /// </summary>
        [JsonPropertyName("deprecated")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Deprecated { get; set; } = false;

        /// <summary>
        /// The schema for the header value.
        /// </summary>
        [JsonPropertyName("schema")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiSchemaMetadata Schema { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty header.
        /// </summary>
        public OpenApiHeaderMetadata()
        {
        }

        /// <summary>
        /// Instantiates a header with the specified values.
        /// </summary>
        /// <param name="description">A brief description of the header.</param>
        /// <param name="schema">The schema for the header value.</param>
        /// <param name="required">Whether the header is required.</param>
        public OpenApiHeaderMetadata(string description, OpenApiSchemaMetadata schema = null, bool required = false)
        {
            Description = description;
            Schema = schema ?? OpenApiSchemaMetadata.String();
            Required = required;
        }

        #endregion
    }
}
