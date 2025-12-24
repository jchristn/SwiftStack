namespace SwiftStack.Rest.OpenApi
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for RestApp to enable OpenAPI/Swagger support.
    /// </summary>
    public static class RestAppExtensions
    {
        /// <summary>
        /// Enables OpenAPI documentation and Swagger UI for the REST application.
        /// </summary>
        /// <param name="app">The REST application.</param>
        /// <param name="configure">Optional action to configure OpenAPI settings.</param>
        /// <returns>The REST application for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when app is null.</exception>
        public static RestApp UseOpenApi(this RestApp app, Action<OpenApiSettings> configure = null)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            OpenApiSettings settings = new OpenApiSettings();
            configure?.Invoke(settings);

            // Store settings in the app
            app.OpenApiSettings = settings;

            // Only register endpoints if OpenAPI is enabled
            if (settings.EnableOpenApi)
            {
                // Register OpenAPI JSON endpoint (unauthenticated)
                app.Get(settings.DocumentPath, async (req) =>
                {
                    string json = app.GenerateOpenApiDocument();
                    req.Http.Response.ContentType = "application/json";
                    return json;
                },
                openApi: api => api
                    .WithTag("OpenAPI")
                    .WithSummary("OpenAPI specification")
                    .WithDescription("Returns the OpenAPI 3.0 specification document in JSON format")
                    .WithResponse(200, OpenApiResponseMetadata.Json(
                        "OpenAPI specification",
                        new OpenApiSchemaMetadata { Type = "object", Description = "OpenAPI 3.0 specification" })));

                // Register Swagger UI endpoint (unauthenticated) if enabled
                if (settings.EnableSwaggerUi)
                {
                    app.Get(settings.SwaggerUiPath, async (req) =>
                    {
                        string html = SwaggerUiHandler.GenerateHtml(
                            settings.DocumentPath,
                            settings.Info.Title,
                            settings.SwaggerUiVersion);
                        req.Http.Response.ContentType = "text/html";
                        return html;
                    },
                    openApi: api => api
                        .WithTag("OpenAPI")
                        .WithSummary("Swagger UI")
                        .WithDescription("Interactive API documentation interface")
                        .WithResponse(200, OpenApiResponseMetadata.Create("Swagger UI HTML page")));
                }
            }

            return app;
        }
    }
}
