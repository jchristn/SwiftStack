namespace SwiftStack.Rest.OpenApi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Allows referencing an external resource for extended documentation.
    /// </summary>
    public class OpenApiExternalDocs
    {
        #region Public-Members

        /// <summary>
        /// A short description of the target documentation.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// The URL for the target documentation.
        /// Required.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates empty external documentation.
        /// </summary>
        public OpenApiExternalDocs()
        {
        }

        /// <summary>
        /// Instantiates external documentation with the specified values.
        /// </summary>
        /// <param name="url">The URL for the target documentation.</param>
        /// <param name="description">A short description of the target documentation.</param>
        public OpenApiExternalDocs(string url, string description = null)
        {
            Url = url;
            Description = description;
        }

        #endregion
    }
}
