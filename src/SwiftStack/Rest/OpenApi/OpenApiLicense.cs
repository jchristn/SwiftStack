namespace SwiftStack.Rest.OpenApi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// License information for the exposed API.
    /// </summary>
    public class OpenApiLicense
    {
        #region Public-Members

        /// <summary>
        /// The license name used for the API.
        /// Required.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "MIT";

        /// <summary>
        /// A URL to the license used for the API.
        /// </summary>
        [JsonPropertyName("url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Url { get; set; } = null;

        /// <summary>
        /// An SPDX license expression for the API.
        /// </summary>
        [JsonPropertyName("identifier")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Identifier { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty license with default MIT name.
        /// </summary>
        public OpenApiLicense()
        {
        }

        /// <summary>
        /// Instantiates a license with the specified values.
        /// </summary>
        /// <param name="name">The license name used for the API.</param>
        /// <param name="url">A URL to the license used for the API.</param>
        public OpenApiLicense(string name, string url = null)
        {
            Name = name;
            Url = url;
        }

        #endregion
    }
}
