namespace SwiftStack.Rest.OpenApi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Each Media Type Object provides schema and examples for the media type identified by its key.
    /// </summary>
    public class OpenApiMediaType
    {
        #region Public-Members

        /// <summary>
        /// The schema defining the content of the request, response, or parameter.
        /// </summary>
        [JsonPropertyName("schema")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiSchemaMetadata Schema { get; set; } = null;

        /// <summary>
        /// Example of the media type.
        /// </summary>
        [JsonPropertyName("example")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Example { get; set; } = null;

        /// <summary>
        /// Examples of the media type.
        /// </summary>
        [JsonPropertyName("examples")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, OpenApiExample> Examples { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty media type.
        /// </summary>
        public OpenApiMediaType()
        {
        }

        /// <summary>
        /// Instantiates a media type with the specified schema.
        /// </summary>
        /// <param name="schema">The schema defining the content.</param>
        public OpenApiMediaType(OpenApiSchemaMetadata schema)
        {
            Schema = schema;
        }

        #endregion
    }

    /// <summary>
    /// An example associated with a media type.
    /// </summary>
    public class OpenApiExample
    {
        #region Public-Members

        /// <summary>
        /// Short description for the example.
        /// </summary>
        [JsonPropertyName("summary")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Summary { get; set; } = null;

        /// <summary>
        /// Long description for the example.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// Embedded literal example value.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Value { get; set; } = null;

        /// <summary>
        /// A URL that points to the literal example.
        /// </summary>
        [JsonPropertyName("externalValue")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ExternalValue { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty example.
        /// </summary>
        public OpenApiExample()
        {
        }

        /// <summary>
        /// Instantiates an example with the specified value.
        /// </summary>
        /// <param name="value">Embedded literal example value.</param>
        /// <param name="summary">Short description for the example.</param>
        public OpenApiExample(object value, string summary = null)
        {
            Value = value;
            Summary = summary;
        }

        #endregion
    }
}
