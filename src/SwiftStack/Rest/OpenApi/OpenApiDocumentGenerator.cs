namespace SwiftStack.Rest.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;
    using WatsonWebserver.Core;

    /// <summary>
    /// Generates OpenAPI 3.0 JSON documents from registered routes.
    /// </summary>
    public class OpenApiDocumentGenerator
    {
        #region Private-Members

        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly Regex _PathParameterRegex = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates a new document generator.
        /// </summary>
        public OpenApiDocumentGenerator()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Generates an OpenAPI 3.0 JSON document from the provided routes and settings.
        /// </summary>
        /// <param name="routes">The routes to document.</param>
        /// <param name="settings">The OpenAPI settings.</param>
        /// <returns>The OpenAPI JSON document as a string.</returns>
        public string Generate(IEnumerable<RouteInfo> routes, OpenApiSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            Dictionary<string, object> document = new Dictionary<string, object>
            {
                ["openapi"] = "3.0.3",
                ["info"] = BuildInfo(settings.Info)
            };

            // Add servers
            if (settings.Servers != null && settings.Servers.Count > 0)
            {
                document["servers"] = settings.Servers;
            }

            // Add paths
            Dictionary<string, Dictionary<string, object>> paths = BuildPaths(routes, settings);
            if (paths.Count > 0)
            {
                document["paths"] = paths;
            }

            // Add components
            Dictionary<string, object> components = BuildComponents(settings);
            if (components.Count > 0)
            {
                document["components"] = components;
            }

            // Add tags
            if (settings.Tags != null && settings.Tags.Count > 0)
            {
                document["tags"] = settings.Tags;
            }

            // Add external docs
            if (settings.ExternalDocs != null)
            {
                document["externalDocs"] = settings.ExternalDocs;
            }

            // Add security
            if (settings.Security != null && settings.Security.Count > 0)
            {
                document["security"] = settings.Security;
            }

            return JsonSerializer.Serialize(document, _JsonOptions);
        }

        #endregion

        #region Private-Methods

        private object BuildInfo(OpenApiInfo info)
        {
            Dictionary<string, object> infoObj = new Dictionary<string, object>
            {
                ["title"] = info.Title ?? "API",
                ["version"] = info.Version ?? "1.0.0"
            };

            if (!string.IsNullOrEmpty(info.Summary))
                infoObj["summary"] = info.Summary;

            if (!string.IsNullOrEmpty(info.Description))
                infoObj["description"] = info.Description;

            if (!string.IsNullOrEmpty(info.TermsOfService))
                infoObj["termsOfService"] = info.TermsOfService;

            if (info.Contact != null)
                infoObj["contact"] = info.Contact;

            if (info.License != null)
                infoObj["license"] = info.License;

            return infoObj;
        }

        private Dictionary<string, Dictionary<string, object>> BuildPaths(IEnumerable<RouteInfo> routes, OpenApiSettings settings)
        {
            Dictionary<string, Dictionary<string, object>> paths = new Dictionary<string, Dictionary<string, object>>();

            if (routes == null)
                return paths;

            foreach (RouteInfo route in routes)
            {
                // Filter based on settings
                if (route.RequiresAuthentication && !settings.IncludeAuthenticatedRoutes)
                    continue;
                if (!route.RequiresAuthentication && !settings.IncludeUnauthenticatedRoutes)
                    continue;

                // Normalize path
                string path = NormalizePath(route.Path);

                // Get or create path item
                if (!paths.TryGetValue(path, out Dictionary<string, object> pathItem))
                {
                    pathItem = new Dictionary<string, object>();
                    paths[path] = pathItem;
                }

                // Add operation
                string method = route.Method.ToString().ToLowerInvariant();
                object operation = BuildOperation(route, settings);
                pathItem[method] = operation;
            }

            return paths;
        }

        private object BuildOperation(RouteInfo route, OpenApiSettings settings)
        {
            OpenApiRouteMetadata metadata = route.OpenApiMetadata ?? GenerateDefaultMetadata(route);

            Dictionary<string, object> operation = new Dictionary<string, object>();

            // Tags
            if (metadata.Tags != null && metadata.Tags.Count > 0)
            {
                operation["tags"] = metadata.Tags;
            }

            // Summary
            if (!string.IsNullOrEmpty(metadata.Summary))
            {
                operation["summary"] = metadata.Summary;
            }

            // Description
            if (!string.IsNullOrEmpty(metadata.Description))
            {
                operation["description"] = metadata.Description;
            }

            // Operation ID
            if (!string.IsNullOrEmpty(metadata.OperationId))
            {
                operation["operationId"] = metadata.OperationId;
            }
            else
            {
                operation["operationId"] = GenerateOperationId(route.Method, route.Path);
            }

            // External docs
            if (metadata.ExternalDocs != null)
            {
                operation["externalDocs"] = metadata.ExternalDocs;
            }

            // Parameters
            List<OpenApiParameterMetadata> parameters = GetParameters(route, metadata);
            if (parameters.Count > 0)
            {
                operation["parameters"] = parameters;
            }

            // Request body
            OpenApiRequestBodyMetadata requestBody = GetRequestBody(route, metadata);
            if (requestBody != null)
            {
                operation["requestBody"] = requestBody;
            }

            // Responses
            Dictionary<string, OpenApiResponseMetadata> responses = GetResponses(route, metadata);
            operation["responses"] = responses;

            // Security
            if (metadata.Security != null && metadata.Security.Count > 0)
            {
                operation["security"] = metadata.Security;
            }
            else if (route.RequiresAuthentication && settings.SecuritySchemes.Count > 0)
            {
                // Auto-add security for authenticated routes
                List<Dictionary<string, List<string>>> security = new List<Dictionary<string, List<string>>>();
                foreach (string schemeName in settings.SecuritySchemes.Keys)
                {
                    security.Add(new Dictionary<string, List<string>>
                    {
                        [schemeName] = new List<string>()
                    });
                }
                operation["security"] = security;
            }

            // Deprecated
            if (metadata.Deprecated)
            {
                operation["deprecated"] = true;
            }

            return operation;
        }

        private OpenApiRouteMetadata GenerateDefaultMetadata(RouteInfo route)
        {
            OpenApiRouteMetadata metadata = new OpenApiRouteMetadata
            {
                Summary = $"{route.Method} {route.Path}"
            };

            return metadata;
        }

        private List<OpenApiParameterMetadata> GetParameters(RouteInfo route, OpenApiRouteMetadata metadata)
        {
            List<OpenApiParameterMetadata> parameters = new List<OpenApiParameterMetadata>();

            // Add explicitly defined parameters
            if (metadata.Parameters != null)
            {
                parameters.AddRange(metadata.Parameters);
            }

            // Auto-extract path parameters if not already defined
            MatchCollection matches = _PathParameterRegex.Matches(route.Path);
            HashSet<string> existingParams = new HashSet<string>(
                parameters.Where(p => p.In == "path").Select(p => p.Name),
                StringComparer.OrdinalIgnoreCase);

            foreach (Match match in matches)
            {
                string paramName = match.Groups[1].Value;
                if (!existingParams.Contains(paramName))
                {
                    parameters.Add(OpenApiParameterMetadata.Path(paramName));
                }
            }

            return parameters;
        }

        private OpenApiRequestBodyMetadata GetRequestBody(RouteInfo route, OpenApiRouteMetadata metadata)
        {
            // Return explicitly defined request body
            if (metadata.RequestBody != null)
            {
                return metadata.RequestBody;
            }

            // Auto-generate for methods that typically have request bodies
            if (route.RequestBodyType != null &&
                route.RequestBodyType != typeof(object) &&
                (route.Method == HttpMethod.POST ||
                 route.Method == HttpMethod.PUT ||
                 route.Method == HttpMethod.PATCH))
            {
                return OpenApiRequestBodyMetadata.Json(
                    OpenApiSchemaMetadata.FromType(route.RequestBodyType),
                    null,
                    true);
            }

            return null;
        }

        private Dictionary<string, OpenApiResponseMetadata> GetResponses(RouteInfo route, OpenApiRouteMetadata metadata)
        {
            Dictionary<string, OpenApiResponseMetadata> responses = new Dictionary<string, OpenApiResponseMetadata>();

            // Add explicitly defined responses
            if (metadata.Responses != null)
            {
                foreach (KeyValuePair<string, OpenApiResponseMetadata> kvp in metadata.Responses)
                {
                    responses[kvp.Key] = kvp.Value;
                }
            }

            // Ensure at least a 200 response exists
            if (!responses.ContainsKey("200") && !responses.ContainsKey("201") && !responses.ContainsKey("204"))
            {
                if (route.ResponseType != null && route.ResponseType != typeof(object))
                {
                    responses["200"] = OpenApiResponseMetadata.Json(
                        "Successful response",
                        OpenApiSchemaMetadata.FromType(route.ResponseType));
                }
                else
                {
                    responses["200"] = OpenApiResponseMetadata.Create("Successful response");
                }
            }

            // Add default error responses if not already present
            if (route.RequiresAuthentication && !responses.ContainsKey("401"))
            {
                responses["401"] = OpenApiResponseMetadata.Unauthorized();
            }

            return responses;
        }

        private Dictionary<string, object> BuildComponents(OpenApiSettings settings)
        {
            Dictionary<string, object> components = new Dictionary<string, object>();

            // Add security schemes
            if (settings.SecuritySchemes != null && settings.SecuritySchemes.Count > 0)
            {
                components["securitySchemes"] = settings.SecuritySchemes;
            }

            return components;
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "/";

            // Ensure path starts with /
            if (!path.StartsWith("/"))
                path = "/" + path;

            // Remove trailing slash (except for root)
            if (path.Length > 1 && path.EndsWith("/"))
                path = path.TrimEnd('/');

            return path;
        }

        private string GenerateOperationId(HttpMethod method, string path)
        {
            // Convert path like /users/{id}/posts to usersIdPosts
            string normalized = path.Replace("{", "").Replace("}", "");
            string[] parts = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            string operationId = method.ToString().ToLowerInvariant();
            foreach (string part in parts)
            {
                // Capitalize first letter
                if (part.Length > 0)
                {
                    operationId += char.ToUpperInvariant(part[0]) + part.Substring(1);
                }
            }

            return operationId;
        }

        #endregion
    }
}
