namespace SwiftStack.Rest.OpenApi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Provides metadata about the API.
    /// </summary>
    public class OpenApiInfo
    {
        #region Public-Members

        /// <summary>
        /// The title of the API.
        /// Required.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = "API";

        /// <summary>
        /// The version of the OpenAPI document.
        /// Required.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// A short summary of the API.
        /// </summary>
        [JsonPropertyName("summary")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Summary { get; set; } = null;

        /// <summary>
        /// A description of the API.
        /// CommonMark syntax MAY be used for rich text representation.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// A URL to the Terms of Service for the API.
        /// </summary>
        [JsonPropertyName("termsOfService")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TermsOfService { get; set; } = null;

        /// <summary>
        /// The contact information for the exposed API.
        /// </summary>
        [JsonPropertyName("contact")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiContact Contact { get; set; } = null;

        /// <summary>
        /// The license information for the exposed API.
        /// </summary>
        [JsonPropertyName("license")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiLicense License { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty info object with default title and version.
        /// </summary>
        public OpenApiInfo()
        {
        }

        /// <summary>
        /// Instantiates an info object with the specified values.
        /// </summary>
        /// <param name="title">The title of the API.</param>
        /// <param name="version">The version of the OpenAPI document.</param>
        /// <param name="description">A description of the API.</param>
        public OpenApiInfo(string title, string version = "1.0.0", string description = null)
        {
            Title = title;
            Version = version;
            Description = description;
        }

        #endregion
    }
}
