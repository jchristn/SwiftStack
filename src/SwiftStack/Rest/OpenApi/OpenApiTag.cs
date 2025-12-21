namespace SwiftStack.Rest.OpenApi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Adds metadata to a single tag that is used by the Operation Object.
    /// </summary>
    public class OpenApiTag
    {
        #region Public-Members

        /// <summary>
        /// The name of the tag.
        /// Required.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// A short description for the tag.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// Additional external documentation for this tag.
        /// </summary>
        [JsonPropertyName("externalDocs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiExternalDocs ExternalDocs { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty tag.
        /// </summary>
        public OpenApiTag()
        {
        }

        /// <summary>
        /// Instantiates a tag with the specified values.
        /// </summary>
        /// <param name="name">The name of the tag.</param>
        /// <param name="description">A short description for the tag.</param>
        public OpenApiTag(string name, string description = null)
        {
            Name = name;
            Description = description;
        }

        #endregion
    }
}
