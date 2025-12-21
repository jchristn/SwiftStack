namespace SwiftStack.Rest.OpenApi
{
    using System.Text;

    /// <summary>
    /// Generates the Swagger UI HTML page that renders OpenAPI documentation.
    /// </summary>
    public static class SwaggerUiHandler
    {
        #region Public-Methods

        /// <summary>
        /// Generates the HTML for the Swagger UI page.
        /// </summary>
        /// <param name="openApiUrl">The URL to the OpenAPI JSON document.</param>
        /// <param name="title">The title of the API.</param>
        /// <param name="swaggerUiVersion">The version of Swagger UI to use. Default is "5.11.0".</param>
        /// <returns>The HTML content for the Swagger UI page.</returns>
        public static string GenerateHtml(string openApiUrl, string title = "API Documentation", string swaggerUiVersion = "5.11.0")
        {
            StringBuilder html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("  <meta charset=\"UTF-8\">");
            html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine($"  <title>{EscapeHtml(title)}</title>");
            html.AppendLine($"  <link rel=\"stylesheet\" type=\"text/css\" href=\"https://unpkg.com/swagger-ui-dist@{swaggerUiVersion}/swagger-ui.css\" />");
            html.AppendLine("  <style>");
            html.AppendLine("    html { box-sizing: border-box; overflow: -moz-scrollbars-vertical; overflow-y: scroll; }");
            html.AppendLine("    *, *:before, *:after { box-sizing: inherit; }");
            html.AppendLine("    body { margin: 0; background: #fafafa; }");
            html.AppendLine("    .swagger-ui .topbar { display: none; }");
            html.AppendLine("  </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("  <div id=\"swagger-ui\"></div>");
            html.AppendLine($"  <script src=\"https://unpkg.com/swagger-ui-dist@{swaggerUiVersion}/swagger-ui-bundle.js\" charset=\"UTF-8\"></script>");
            html.AppendLine($"  <script src=\"https://unpkg.com/swagger-ui-dist@{swaggerUiVersion}/swagger-ui-standalone-preset.js\" charset=\"UTF-8\"></script>");
            html.AppendLine("  <script>");
            html.AppendLine("    window.onload = function() {");
            html.AppendLine("      const ui = SwaggerUIBundle({");
            html.AppendLine($"        url: \"{EscapeJs(openApiUrl)}\",");
            html.AppendLine("        dom_id: '#swagger-ui',");
            html.AppendLine("        deepLinking: true,");
            html.AppendLine("        presets: [");
            html.AppendLine("          SwaggerUIBundle.presets.apis,");
            html.AppendLine("          SwaggerUIStandalonePreset");
            html.AppendLine("        ],");
            html.AppendLine("        plugins: [");
            html.AppendLine("          SwaggerUIBundle.plugins.DownloadUrl");
            html.AppendLine("        ],");
            html.AppendLine("        layout: \"StandaloneLayout\",");
            html.AppendLine("        defaultModelsExpandDepth: 1,");
            html.AppendLine("        defaultModelExpandDepth: 1,");
            html.AppendLine("        docExpansion: \"list\",");
            html.AppendLine("        filter: true,");
            html.AppendLine("        showExtensions: true,");
            html.AppendLine("        showCommonExtensions: true,");
            html.AppendLine("        tryItOutEnabled: true,");
            html.AppendLine("        persistAuthorization: true");
            html.AppendLine("      });");
            html.AppendLine("      window.ui = ui;");
            html.AppendLine("    };");
            html.AppendLine("  </script>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        #endregion

        #region Private-Methods

        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        private static string EscapeJs(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("'", "\\'")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
        }

        #endregion
    }
}
