namespace SwiftStack.Rest.OpenApi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAPI configuration settings for the API.
    /// </summary>
    public class OpenApiSettings
    {
        #region Public-Members

        /// <summary>
        /// Provides metadata about the API.
        /// </summary>
        public OpenApiInfo Info
        {
            get { return _Info; }
            set { _Info = value ?? new OpenApiInfo(); }
        }

        /// <summary>
        /// An array of Server Objects, which provide connectivity information to a target server.
        /// </summary>
        public List<OpenApiServer> Servers
        {
            get { return _Servers; }
            set { _Servers = value ?? new List<OpenApiServer>(); }
        }

        /// <summary>
        /// A list of tags used by the specification with additional metadata.
        /// </summary>
        public List<OpenApiTag> Tags
        {
            get { return _Tags; }
            set { _Tags = value ?? new List<OpenApiTag>(); }
        }

        /// <summary>
        /// Additional external documentation.
        /// </summary>
        public OpenApiExternalDocs ExternalDocs { get; set; } = null;

        /// <summary>
        /// A map of security schemes that can be used across the specification.
        /// </summary>
        public Dictionary<string, OpenApiSecurityScheme> SecuritySchemes
        {
            get { return _SecuritySchemes; }
            set { _SecuritySchemes = value ?? new Dictionary<string, OpenApiSecurityScheme>(); }
        }

        /// <summary>
        /// The path at which the OpenAPI JSON document will be served.
        /// Default is "/openapi.json".
        /// </summary>
        public string DocumentPath
        {
            get { return _DocumentPath; }
            set { _DocumentPath = string.IsNullOrWhiteSpace(value) ? "/openapi.json" : value; }
        }

        /// <summary>
        /// The path at which the Swagger UI will be served.
        /// Default is "/swagger".
        /// </summary>
        public string SwaggerUiPath
        {
            get { return _SwaggerUiPath; }
            set { _SwaggerUiPath = string.IsNullOrWhiteSpace(value) ? "/swagger" : value; }
        }

        /// <summary>
        /// Whether to enable the Swagger UI.
        /// Default is true.
        /// </summary>
        public bool EnableSwaggerUi { get; set; } = true;

        /// <summary>
        /// Whether to include unauthenticated routes in the documentation.
        /// Default is true.
        /// </summary>
        public bool IncludeUnauthenticatedRoutes { get; set; } = true;

        /// <summary>
        /// Whether to include authenticated routes in the documentation.
        /// Default is true.
        /// </summary>
        public bool IncludeAuthenticatedRoutes { get; set; } = true;

        /// <summary>
        /// The version of Swagger UI to use.
        /// Default is "5.11.0".
        /// </summary>
        public string SwaggerUiVersion
        {
            get { return _SwaggerUiVersion; }
            set { _SwaggerUiVersion = string.IsNullOrWhiteSpace(value) ? "5.11.0" : value; }
        }

        /// <summary>
        /// A security requirement that applies to all operations unless overridden.
        /// </summary>
        public List<Dictionary<string, List<string>>> Security
        {
            get { return _Security; }
            set { _Security = value ?? new List<Dictionary<string, List<string>>>(); }
        }

        #endregion

        #region Private-Members

        private OpenApiInfo _Info = new OpenApiInfo();
        private List<OpenApiServer> _Servers = new List<OpenApiServer>();
        private List<OpenApiTag> _Tags = new List<OpenApiTag>();
        private Dictionary<string, OpenApiSecurityScheme> _SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>();
        private string _DocumentPath = "/openapi.json";
        private string _SwaggerUiPath = "/swagger";
        private string _SwaggerUiVersion = "5.11.0";
        private List<Dictionary<string, List<string>>> _Security = new List<Dictionary<string, List<string>>>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates OpenAPI settings with defaults.
        /// </summary>
        public OpenApiSettings()
        {
        }

        /// <summary>
        /// Instantiates OpenAPI settings with the specified title and version.
        /// </summary>
        /// <param name="title">The title of the API.</param>
        /// <param name="version">The version of the API.</param>
        public OpenApiSettings(string title, string version = "1.0.0")
        {
            _Info = new OpenApiInfo(title, version);
        }

        #endregion
    }
}
