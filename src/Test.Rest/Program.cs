namespace Test.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Metadata;
    using System.Threading;
    using System.Threading.Tasks;
    using SerializationHelper;
    using SwiftStack;
    using SwiftStack.Rest;
    using SwiftStack.Rest.OpenApi;
    using WatsonWebserver.Core;

    public static class Program
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private static Serializer _Serializer = new Serializer();

        public static async Task Main(string[] args)
        {
            SwiftStackApp app = new SwiftStackApp("My test application", false);

            #region REST

            #region OpenAPI-Configuration

            app.Rest.UseOpenApi(openApi =>
            {
                openApi.Info.Title = "SwiftStack Test API";
                openApi.Info.Version = "1.0.0";
                openApi.Info.Description = "Test API demonstrating SwiftStack REST capabilities with OpenAPI documentation.";
                openApi.Info.Contact = new OpenApiContact("SwiftStack", "support@swiftstack.io");

                // Define tags for route grouping
                openApi.Tags.Add(new OpenApiTag("General", "General test endpoints"));
                openApi.Tags.Add(new OpenApiTag("Users", "User management endpoints"));
                openApi.Tags.Add(new OpenApiTag("Types", "Response type demonstrations"));
                openApi.Tags.Add(new OpenApiTag("Exceptions", "Exception handling test endpoints"));
                openApi.Tags.Add(new OpenApiTag("Authentication", "Authenticated endpoints"));

                // Define security schemes
                openApi.SecuritySchemes["Bearer"] = OpenApiSecurityScheme.Bearer("JWT", "Use 'password' as the bearer token for testing");
                openApi.SecuritySchemes["Basic"] = OpenApiSecurityScheme.Basic("Use user:password for testing");
            });

            #endregion

            #region Unauthenticated-Routes

            app.Rest.Get("/", async (req) => "Hello, unauthenticated user",
                api => api.WithTag("General").WithSummary("Root endpoint").WithDescription("Returns a simple greeting message"));

            app.Rest.Get("/null-200", async (req) => null);

            app.Rest.Get("/null-204", async (req) =>
            {
                req.Http.Response.StatusCode = 204;
                return null;
            });

            app.Rest.Post<string>("/loopback", async (req) => req.Data);

            app.Rest.Get("/search", async (req) =>
            {
                string query = req.Query["q"];
                if (query == null) query = "no query provided";
                int page = int.TryParse(req.Query["page"] as string, out int p) ? p : 1;

                return new
                {
                    Query = query,
                    Page = page,
                    Message = $"Searching for '{query}' on page {page}"
                };
            },
            api => api
                .WithTag("General")
                .WithSummary("Search endpoint")
                .WithDescription("Demonstrates query parameter handling with pagination")
                .WithParameter(OpenApiParameterMetadata.Query("q", "Search query string", false))
                .WithParameter(OpenApiParameterMetadata.Query("page", "Page number (default: 1)", false, OpenApiSchemaMetadata.Integer())));

            app.Rest.Get("/user", async (req) =>
            {
                return new
                {
                    Email = "foo@bar.com",
                    Password = "password"
                };
            },
            api => api
                .WithTag("Users")
                .WithSummary("Get sample user")
                .WithDescription("Returns a sample user object")
                .WithResponse(200, OpenApiResponseMetadata.Json<User>("Sample user data")));

            app.Rest.Put<User>("/user/{id}", async (req) =>
            {
                string id = req.Parameters["id"];
                User user = req.GetData<User>();

                return new
                {
                    Id = id,
                    Email = user.Email,
                    Password = user.Password
                };
            },
            api => api
                .WithTag("Users")
                .WithSummary("Update user by ID")
                .WithDescription("Updates a user's information by their ID")
                .WithParameter(OpenApiParameterMetadata.Path("id", "User ID"))
                .WithRequestBody(OpenApiRequestBodyMetadata.Json<User>("User data to update", true))
                .WithResponse(200, OpenApiResponseMetadata.Json<User>("Updated user")));

            app.Rest.Get("/types/{type}", async (req) =>
            {
                string type = req.Parameters["type"].ToString().ToLower();

                switch (type)
                {
                    case "string":
                        return "This is a simple string response";

                    case "number":
                        return 42;

                    case "json":
                        return new { Message = "This is a JSON response", Timestamp = DateTime.UtcNow };

                    case "null":
                        return null; // Will return 204 No Content

                    default:
                        throw new SwiftStackException(ApiResultEnum.NotFound);
                }
            });

            app.Rest.Get("/events/{count}", async (req) =>
            {
                int count = Convert.ToInt32(req.Parameters["count"].ToString());

                req.Http.Response.ServerSentEvents = true;

                for (int i = 0; i < count; i++)
                {
                    await req.Http.Response.SendEvent(
                        new ServerSentEvent 
                        {
                            Data = ("Event " + i)
                        }, 
                        false);
                    await Task.Delay(500);
                }

                await req.Http.Response.SendEvent(new ServerSentEvent { Data = "" }, true);

                return null;
            });

            app.Rest.Get("/exception/400", async (req) =>
            {
                throw new SwiftStackException(ApiResultEnum.BadRequest);
            },
            api => api.WithTag("Exceptions").WithSummary("400 Bad Request").WithResponse(400, OpenApiResponseMetadata.BadRequest()));

            app.Rest.Get("/exception/401", async (req) =>
            {
                throw new SwiftStackException(ApiResultEnum.NotAuthorized);
            },
            api => api.WithTag("Exceptions").WithSummary("401 Unauthorized").WithResponse(401, OpenApiResponseMetadata.Unauthorized()));

            app.Rest.Get("/exception/404", async (req) =>
            {
                throw new SwiftStackException(ApiResultEnum.NotFound);
            },
            api => api.WithTag("Exceptions").WithSummary("404 Not Found").WithResponse(404, OpenApiResponseMetadata.NotFound()));

            app.Rest.Get("/exception/409", async (req) =>
            {
                throw new SwiftStackException(ApiResultEnum.Conflict);
            },
            api => api.WithTag("Exceptions").WithSummary("409 Conflict").WithResponse(409, OpenApiResponseMetadata.Conflict()));

            app.Rest.Get("/exception/500", async (req) =>
            {
                throw new SwiftStackException(ApiResultEnum.InternalError);
            },
            api => api.WithTag("Exceptions").WithSummary("500 Internal Server Error").WithResponse(500, OpenApiResponseMetadata.InternalServerError()));

            #endregion

            #region Authenticated-Routes

            app.Rest.AuthenticationRoute = AuthenticationRoute;

            app.Rest.Get("/authenticated", async (req) =>
            {
                Console.WriteLine("HTTP context metadata: " + Environment.NewLine + _Serializer.SerializeJson(req.Http.Metadata, true));
                return "Hello, authenticated user";
            },
            api => api
                .WithTag("Authentication")
                .WithSummary("Authenticated endpoint")
                .WithDescription("Demonstrates authenticated access using Bearer or Basic auth")
                .WithSecurity("Bearer")
                .WithSecurity("Basic")
                .WithResponse(200, OpenApiResponseMetadata.Text("Success message"))
                .WithResponse(401, OpenApiResponseMetadata.Unauthorized()),
            requireAuthentication: true);

            #endregion

            Task rest = Task.Run(() => app.Rest.Run());

            #endregion

            Console.WriteLine("REST API running on http://localhost:8080");
            Console.WriteLine();
            Console.WriteLine("OpenAPI Documentation:");
            Console.WriteLine("  - OpenAPI JSON: http://localhost:8080/openapi.json");
            Console.WriteLine("  - Swagger UI:   http://localhost:8080/swagger");
            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }

        private static async Task<AuthResult> AuthenticationRoute(HttpContextBase ctx)
        {
            if (ctx.Request.Authorization != null)
            {
                if (!String.IsNullOrEmpty(ctx.Request.Authorization.Username)
                    && !String.IsNullOrEmpty(ctx.Request.Authorization.Password)
                    && ctx.Request.Authorization.Username.Equals("user")
                    && ctx.Request.Authorization.Password.Equals("password"))
                {
                    ctx.Metadata = new
                    {
                        Authorized = true,
                        Method = "credentials"
                    };

                    return new AuthResult
                    {
                        AuthenticationResult = AuthenticationResultEnum.Success,
                        AuthorizationResult = AuthorizationResultEnum.Permitted
                    };
                }
                else if (!String.IsNullOrEmpty(ctx.Request.Authorization.BearerToken)
                    && ctx.Request.Authorization.BearerToken.Equals("password"))
                {
                    ctx.Metadata = new
                    {
                        Authorized = true,
                        Method = "bearer"
                    };

                    return new AuthResult
                    {
                        AuthenticationResult = AuthenticationResultEnum.Success,
                        AuthorizationResult = AuthorizationResultEnum.Permitted
                    };
                }
            }

            return new AuthResult
            {
                AuthenticationResult = AuthenticationResultEnum.NotFound,
                AuthorizationResult = AuthorizationResultEnum.DeniedImplicit
            };
        }

        public class User
        {
            public string Id { get; set; } = null;
            public string Email { get; set; } = null;
            public string Password { get; set; } = null;
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}