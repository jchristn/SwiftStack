namespace SwiftStack.Rest.OpenApi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// An object representing a Server.
    /// </summary>
    public class OpenApiServer
    {
        #region Public-Members

        /// <summary>
        /// A URL to the target host.
        /// Required.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = null;

        /// <summary>
        /// An optional string describing the host designated by the URL.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// A map between a variable name and its value used for substitution in the server's URL template.
        /// </summary>
        [JsonPropertyName("variables")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, OpenApiServerVariable> Variables { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty server.
        /// </summary>
        public OpenApiServer()
        {
        }

        /// <summary>
        /// Instantiates a server with the specified values.
        /// </summary>
        /// <param name="url">A URL to the target host.</param>
        /// <param name="description">An optional string describing the host designated by the URL.</param>
        public OpenApiServer(string url, string description = null)
        {
            Url = url;
            Description = description;
        }

        #endregion
    }

    /// <summary>
    /// An object representing a Server Variable for server URL template substitution.
    /// </summary>
    public class OpenApiServerVariable
    {
        #region Public-Members

        /// <summary>
        /// The default value to use for substitution, which SHALL be sent if an alternate value is not supplied.
        /// Required.
        /// </summary>
        [JsonPropertyName("default")]
        public string Default { get; set; } = null;

        /// <summary>
        /// An optional description for the server variable.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// An enumeration of string values to be used if the substitution options are from a limited set.
        /// </summary>
        [JsonPropertyName("enum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> Enum { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty server variable.
        /// </summary>
        public OpenApiServerVariable()
        {
        }

        /// <summary>
        /// Instantiates a server variable with the specified default value.
        /// </summary>
        /// <param name="defaultValue">The default value to use for substitution.</param>
        /// <param name="description">An optional description for the server variable.</param>
        public OpenApiServerVariable(string defaultValue, string description = null)
        {
            Default = defaultValue;
            Description = description;
        }

        #endregion
    }
}
