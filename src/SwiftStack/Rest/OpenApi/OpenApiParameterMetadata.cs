namespace SwiftStack.Rest.OpenApi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Describes a single operation parameter.
    /// </summary>
    public class OpenApiParameterMetadata
    {
        #region Public-Members

        /// <summary>
        /// The name of the parameter.
        /// Required.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// The location of the parameter.
        /// Required.
        /// </summary>
        [JsonPropertyName("in")]
        public string In { get; set; } = "query";

        /// <summary>
        /// A brief description of the parameter.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// Whether the parameter is required.
        /// If the parameter location is "path", this is required and must be true.
        /// </summary>
        [JsonPropertyName("required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Required { get; set; } = false;

        /// <summary>
        /// Whether the parameter value should be deprecated.
        /// </summary>
        [JsonPropertyName("deprecated")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Deprecated { get; set; } = false;

        /// <summary>
        /// Whether empty values are allowed for this parameter.
        /// </summary>
        [JsonPropertyName("allowEmptyValue")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool AllowEmptyValue { get; set; } = false;

        /// <summary>
        /// The schema defining the type used for the parameter.
        /// </summary>
        [JsonPropertyName("schema")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiSchemaMetadata Schema { get; set; } = null;

        /// <summary>
        /// Example of the parameter's potential value.
        /// </summary>
        [JsonPropertyName("example")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Example { get; set; } = null;

        /// <summary>
        /// Describes how the parameter value will be serialized for arrays and objects.
        /// </summary>
        [JsonPropertyName("style")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Style { get; set; } = null;

        /// <summary>
        /// When true, parameter values of type array or object generate separate parameters
        /// for each value of the array or key-value pair of the map.
        /// </summary>
        [JsonPropertyName("explode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Explode { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty parameter.
        /// </summary>
        public OpenApiParameterMetadata()
        {
        }

        /// <summary>
        /// Instantiates a parameter with the specified values.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="location">The location of the parameter.</param>
        /// <param name="description">A brief description of the parameter.</param>
        /// <param name="required">Whether the parameter is required.</param>
        /// <param name="schema">The schema defining the type used for the parameter.</param>
        public OpenApiParameterMetadata(string name, ParameterLocation location, string description = null, bool required = false, OpenApiSchemaMetadata schema = null)
        {
            Name = name;
            In = LocationToString(location);
            Description = description;
            Required = location == ParameterLocation.Path || required;
            Schema = schema ?? OpenApiSchemaMetadata.String();
        }

        /// <summary>
        /// Creates a path parameter.
        /// Path parameters are always required.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="description">A brief description of the parameter.</param>
        /// <param name="schema">The schema defining the type used for the parameter. Default is string.</param>
        /// <returns>A path parameter.</returns>
        public static OpenApiParameterMetadata Path(string name, string description = null, OpenApiSchemaMetadata schema = null)
        {
            return new OpenApiParameterMetadata
            {
                Name = name,
                In = "path",
                Description = description,
                Required = true,
                Schema = schema ?? OpenApiSchemaMetadata.String()
            };
        }

        /// <summary>
        /// Creates a query parameter.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="description">A brief description of the parameter.</param>
        /// <param name="required">Whether the parameter is required.</param>
        /// <param name="schema">The schema defining the type used for the parameter. Default is string.</param>
        /// <returns>A query parameter.</returns>
        public static OpenApiParameterMetadata Query(string name, string description = null, bool required = false, OpenApiSchemaMetadata schema = null)
        {
            return new OpenApiParameterMetadata
            {
                Name = name,
                In = "query",
                Description = description,
                Required = required,
                Schema = schema ?? OpenApiSchemaMetadata.String()
            };
        }

        /// <summary>
        /// Creates a header parameter.
        /// </summary>
        /// <param name="name">The name of the header.</param>
        /// <param name="description">A brief description of the parameter.</param>
        /// <param name="required">Whether the parameter is required.</param>
        /// <param name="schema">The schema defining the type used for the parameter. Default is string.</param>
        /// <returns>A header parameter.</returns>
        public static OpenApiParameterMetadata Header(string name, string description = null, bool required = false, OpenApiSchemaMetadata schema = null)
        {
            return new OpenApiParameterMetadata
            {
                Name = name,
                In = "header",
                Description = description,
                Required = required,
                Schema = schema ?? OpenApiSchemaMetadata.String()
            };
        }

        /// <summary>
        /// Creates a cookie parameter.
        /// </summary>
        /// <param name="name">The name of the cookie.</param>
        /// <param name="description">A brief description of the parameter.</param>
        /// <param name="required">Whether the parameter is required.</param>
        /// <param name="schema">The schema defining the type used for the parameter. Default is string.</param>
        /// <returns>A cookie parameter.</returns>
        public static OpenApiParameterMetadata Cookie(string name, string description = null, bool required = false, OpenApiSchemaMetadata schema = null)
        {
            return new OpenApiParameterMetadata
            {
                Name = name,
                In = "cookie",
                Description = description,
                Required = required,
                Schema = schema ?? OpenApiSchemaMetadata.String()
            };
        }

        #endregion

        #region Private-Methods

        private static string LocationToString(ParameterLocation location)
        {
            switch (location)
            {
                case ParameterLocation.Path: return "path";
                case ParameterLocation.Query: return "query";
                case ParameterLocation.Header: return "header";
                case ParameterLocation.Cookie: return "cookie";
                default: return "query";
            }
        }

        #endregion
    }
}
