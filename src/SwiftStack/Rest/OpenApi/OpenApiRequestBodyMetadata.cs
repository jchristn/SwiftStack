namespace SwiftStack.Rest.OpenApi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Describes a single request body.
    /// </summary>
    public class OpenApiRequestBodyMetadata
    {
        #region Public-Members

        /// <summary>
        /// A brief description of the request body.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// The content of the request body.
        /// The key is a media type (e.g., "application/json").
        /// Required.
        /// </summary>
        [JsonPropertyName("content")]
        public Dictionary<string, OpenApiMediaType> Content { get; set; } = new Dictionary<string, OpenApiMediaType>();

        /// <summary>
        /// Whether the request body is required.
        /// Default is false.
        /// </summary>
        [JsonPropertyName("required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Required { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty request body.
        /// </summary>
        public OpenApiRequestBodyMetadata()
        {
        }

        /// <summary>
        /// Creates a JSON request body.
        /// </summary>
        /// <param name="schema">The schema for the request body.</param>
        /// <param name="description">A brief description of the request body.</param>
        /// <param name="required">Whether the request body is required.</param>
        /// <returns>A JSON request body.</returns>
        public static OpenApiRequestBodyMetadata Json(OpenApiSchemaMetadata schema, string description = null, bool required = true)
        {
            return new OpenApiRequestBodyMetadata
            {
                Description = description,
                Required = required,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType(schema)
                }
            };
        }

        /// <summary>
        /// Creates a JSON request body from a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to create a schema for.</typeparam>
        /// <param name="description">A brief description of the request body.</param>
        /// <param name="required">Whether the request body is required.</param>
        /// <returns>A JSON request body with schema derived from the type.</returns>
        public static OpenApiRequestBodyMetadata Json<T>(string description = null, bool required = true)
        {
            return Json(OpenApiSchemaMetadata.FromType<T>(), description, required);
        }

        /// <summary>
        /// Creates a form data request body.
        /// </summary>
        /// <param name="schema">The schema for the request body.</param>
        /// <param name="description">A brief description of the request body.</param>
        /// <param name="required">Whether the request body is required.</param>
        /// <returns>A form data request body.</returns>
        public static OpenApiRequestBodyMetadata FormData(OpenApiSchemaMetadata schema, string description = null, bool required = true)
        {
            return new OpenApiRequestBodyMetadata
            {
                Description = description,
                Required = required,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/x-www-form-urlencoded"] = new OpenApiMediaType(schema)
                }
            };
        }

        /// <summary>
        /// Creates a multipart form data request body.
        /// </summary>
        /// <param name="schema">The schema for the request body.</param>
        /// <param name="description">A brief description of the request body.</param>
        /// <param name="required">Whether the request body is required.</param>
        /// <returns>A multipart form data request body.</returns>
        public static OpenApiRequestBodyMetadata MultipartFormData(OpenApiSchemaMetadata schema, string description = null, bool required = true)
        {
            return new OpenApiRequestBodyMetadata
            {
                Description = description,
                Required = required,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType(schema)
                }
            };
        }

        /// <summary>
        /// Creates a plain text request body.
        /// </summary>
        /// <param name="description">A brief description of the request body.</param>
        /// <param name="required">Whether the request body is required.</param>
        /// <returns>A plain text request body.</returns>
        public static OpenApiRequestBodyMetadata Text(string description = null, bool required = true)
        {
            return new OpenApiRequestBodyMetadata
            {
                Description = description,
                Required = required,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["text/plain"] = new OpenApiMediaType(OpenApiSchemaMetadata.String())
                }
            };
        }

        /// <summary>
        /// Creates a binary request body.
        /// </summary>
        /// <param name="description">A brief description of the request body.</param>
        /// <param name="required">Whether the request body is required.</param>
        /// <returns>A binary request body.</returns>
        public static OpenApiRequestBodyMetadata Binary(string description = null, bool required = true)
        {
            return new OpenApiRequestBodyMetadata
            {
                Description = description,
                Required = required,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/octet-stream"] = new OpenApiMediaType(new OpenApiSchemaMetadata { Type = "string", Format = "binary" })
                }
            };
        }

        #endregion
    }
}
