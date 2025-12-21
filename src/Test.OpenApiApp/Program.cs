namespace Test.OpenApiApp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using SwiftStack;
    using SwiftStack.Rest;
    using SwiftStack.Rest.OpenApi;

    public static class Program
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private static List<Product> _Products = new List<Product>
        {
            new Product { Id = "1", Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, Category = "Electronics", InStock = true },
            new Product { Id = "2", Name = "Headphones", Description = "Wireless noise-canceling headphones", Price = 249.99m, Category = "Electronics", InStock = true },
            new Product { Id = "3", Name = "Coffee Maker", Description = "Programmable coffee maker", Price = 79.99m, Category = "Appliances", InStock = false },
            new Product { Id = "4", Name = "Desk Chair", Description = "Ergonomic office chair", Price = 349.99m, Category = "Furniture", InStock = true }
        };

        private static List<User> _Users = new List<User>
        {
            new User { Id = "1", Email = "john@example.com", Name = "John Doe", Role = "admin" },
            new User { Id = "2", Email = "jane@example.com", Name = "Jane Smith", Role = "user" }
        };

        public static async Task Main(string[] args)
        {
            SwiftStackApp app = new SwiftStackApp("OpenAPI Demo Application", false);

            #region OpenAPI-Configuration

            app.Rest.UseOpenApi(openApi =>
            {
                openApi.Info.Title = "SwiftStack Demo API";
                openApi.Info.Version = "1.0.0";
                openApi.Info.Description = "A demonstration API showcasing SwiftStack REST capabilities with full OpenAPI 3.0 documentation.";
                openApi.Info.Contact = new OpenApiContact("SwiftStack Team", "support@swiftstack.io");
                openApi.Info.License = new OpenApiLicense("MIT", "https://opensource.org/licenses/MIT");

                // Define tags for route grouping
                openApi.Tags.Add(new OpenApiTag("Health", "Health check and status endpoints"));
                openApi.Tags.Add(new OpenApiTag("Products", "Product catalog management"));
                openApi.Tags.Add(new OpenApiTag("Users", "User management endpoints"));

                // Define security schemes
                openApi.SecuritySchemes["Bearer"] = OpenApiSecurityScheme.Bearer(
                    "JWT",
                    "Use 'demo-token' as the bearer token for testing");
                openApi.SecuritySchemes["ApiKey"] = OpenApiSecurityScheme.ApiKey(
                    "X-API-Key",
                    "header",
                    "Use 'demo-api-key' as the API key for testing");
            });

            #endregion

            #region Health-Endpoints

            app.Rest.Get("/", async (req) => new
            {
                Name = "SwiftStack Demo API",
                Version = "1.0.0",
                Status = "Running",
                Timestamp = DateTime.UtcNow
            },
            api => api
                .WithTag("Health")
                .WithSummary("API root")
                .WithDescription("Returns basic API information and status"));

            app.Rest.Get("/health", async (req) => new
            {
                Status = "Healthy",
                Uptime = TimeSpan.FromMinutes(42).ToString(),
                Timestamp = DateTime.UtcNow
            },
            api => api
                .WithTag("Health")
                .WithSummary("Health check")
                .WithDescription("Returns the current health status of the API"));

            #endregion

            #region Products-Endpoints

            app.Rest.Get("/products", async (req) =>
            {
                string category = req.Query["category"] as string;
                bool? inStock = req.Query["inStock"] != null
                    ? bool.TryParse(req.Query["inStock"] as string, out bool stock) ? stock : (bool?)null
                    : null;

                IEnumerable<Product> result = _Products;

                if (!string.IsNullOrEmpty(category))
                    result = result.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

                if (inStock.HasValue)
                    result = result.Where(p => p.InStock == inStock.Value);

                return new
                {
                    Count = result.Count(),
                    Products = result.ToList()
                };
            },
            api => api
                .WithTag("Products")
                .WithSummary("List all products")
                .WithDescription("Returns a list of all products, optionally filtered by category or stock status")
                .WithParameter(OpenApiParameterMetadata.Query("category", "Filter by product category", false))
                .WithParameter(OpenApiParameterMetadata.Query("inStock", "Filter by stock availability", false, OpenApiSchemaMetadata.Boolean())));

            app.Rest.Get("/products/{id}", async (req) =>
            {
                string id = req.Parameters["id"];
                Product product = _Products.FirstOrDefault(p => p.Id == id);

                if (product == null)
                    throw new SwiftStackException(ApiResultEnum.NotFound, "Product not found");

                return product;
            },
            api => api
                .WithTag("Products")
                .WithSummary("Get product by ID")
                .WithDescription("Returns a single product by its unique identifier")
                .WithParameter(OpenApiParameterMetadata.Path("id", "The product ID"))
                .WithResponse(200, OpenApiResponseMetadata.Json<Product>("Product details"))
                .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            app.Rest.Post<Product>("/products", async (req) =>
            {
                Product product = req.GetData<Product>();
                product.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
                _Products.Add(product);

                req.Http.Response.StatusCode = 201;
                return product;
            },
            api => api
                .WithTag("Products")
                .WithSummary("Create a new product")
                .WithDescription("Creates a new product and returns the created product with its assigned ID")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json<Product>("Product data", true))
                .WithResponse(201, OpenApiResponseMetadata.Json<Product>("Created product"))
                .WithResponse(400, OpenApiResponseMetadata.BadRequest()));

            app.Rest.Put<Product>("/products/{id}", async (req) =>
            {
                string id = req.Parameters["id"];
                int index = _Products.FindIndex(p => p.Id == id);

                if (index == -1)
                    throw new SwiftStackException(ApiResultEnum.NotFound, "Product not found");

                Product product = req.GetData<Product>();
                product.Id = id;
                _Products[index] = product;

                return product;
            },
            api => api
                .WithTag("Products")
                .WithSummary("Update a product")
                .WithDescription("Updates an existing product by its ID")
                .WithParameter(OpenApiParameterMetadata.Path("id", "The product ID"))
                .WithRequestBody(OpenApiRequestBodyMetadata.Json<Product>("Updated product data", true))
                .WithResponse(200, OpenApiResponseMetadata.Json<Product>("Updated product"))
                .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            app.Rest.Delete("/products/{id}", async (req) =>
            {
                string id = req.Parameters["id"];
                Product product = _Products.FirstOrDefault(p => p.Id == id);

                if (product == null)
                    throw new SwiftStackException(ApiResultEnum.NotFound, "Product not found");

                _Products.Remove(product);

                req.Http.Response.StatusCode = 204;
                return null;
            },
            api => api
                .WithTag("Products")
                .WithSummary("Delete a product")
                .WithDescription("Deletes a product by its ID")
                .WithParameter(OpenApiParameterMetadata.Path("id", "The product ID"))
                .WithResponse(204, OpenApiResponseMetadata.NoContent())
                .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            app.Rest.Get("/products/search", async (req) =>
            {
                string query = req.Query["q"] as string;
                decimal? minPrice = req.Query["minPrice"] != null
                    ? decimal.TryParse(req.Query["minPrice"] as string, out decimal min) ? min : (decimal?)null
                    : null;
                decimal? maxPrice = req.Query["maxPrice"] != null
                    ? decimal.TryParse(req.Query["maxPrice"] as string, out decimal max) ? max : (decimal?)null
                    : null;

                IEnumerable<Product> result = _Products;

                if (!string.IsNullOrEmpty(query))
                    result = result.Where(p =>
                        p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains(query, StringComparison.OrdinalIgnoreCase));

                if (minPrice.HasValue)
                    result = result.Where(p => p.Price >= minPrice.Value);

                if (maxPrice.HasValue)
                    result = result.Where(p => p.Price <= maxPrice.Value);

                return new
                {
                    Query = query,
                    Count = result.Count(),
                    Products = result.ToList()
                };
            },
            api => api
                .WithTag("Products")
                .WithSummary("Search products")
                .WithDescription("Search products by name or description with optional price filtering")
                .WithParameter(OpenApiParameterMetadata.Query("q", "Search query string", false))
                .WithParameter(OpenApiParameterMetadata.Query("minPrice", "Minimum price filter", false, OpenApiSchemaMetadata.Number()))
                .WithParameter(OpenApiParameterMetadata.Query("maxPrice", "Maximum price filter", false, OpenApiSchemaMetadata.Number())));

            #endregion

            #region Users-Endpoints

            app.Rest.AuthenticationRoute = AuthenticationRoute;

            app.Rest.Get("/users", async (req) =>
            {
                return new
                {
                    Count = _Users.Count,
                    Users = _Users.Select(u => new { u.Id, u.Email, u.Name, u.Role }).ToList()
                };
            },
            api => api
                .WithTag("Users")
                .WithSummary("List all users")
                .WithDescription("Returns a list of all users (requires authentication)")
                .WithSecurity("Bearer")
                .WithSecurity("ApiKey")
                .WithResponse(401, OpenApiResponseMetadata.Unauthorized()),
            requireAuthentication: true);

            app.Rest.Get("/users/{id}", async (req) =>
            {
                string id = req.Parameters["id"];
                User user = _Users.FirstOrDefault(u => u.Id == id);

                if (user == null)
                    throw new SwiftStackException(ApiResultEnum.NotFound, "User not found");

                return new { user.Id, user.Email, user.Name, user.Role };
            },
            api => api
                .WithTag("Users")
                .WithSummary("Get user by ID")
                .WithDescription("Returns a single user by their ID (requires authentication)")
                .WithParameter(OpenApiParameterMetadata.Path("id", "The user ID"))
                .WithSecurity("Bearer")
                .WithSecurity("ApiKey")
                .WithResponse(200, OpenApiResponseMetadata.Json<User>("User details"))
                .WithResponse(401, OpenApiResponseMetadata.Unauthorized())
                .WithResponse(404, OpenApiResponseMetadata.NotFound()),
            requireAuthentication: true);

            app.Rest.Post<CreateUserRequest>("/users", async (req) =>
            {
                CreateUserRequest createReq = req.GetData<CreateUserRequest>();

                if (_Users.Any(u => u.Email.Equals(createReq.Email, StringComparison.OrdinalIgnoreCase)))
                    throw new SwiftStackException(ApiResultEnum.Conflict, "User with this email already exists");

                User user = new User
                {
                    Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                    Email = createReq.Email,
                    Name = createReq.Name,
                    Role = createReq.Role ?? "user"
                };

                _Users.Add(user);

                req.Http.Response.StatusCode = 201;
                return new { user.Id, user.Email, user.Name, user.Role };
            },
            api => api
                .WithTag("Users")
                .WithSummary("Create a new user")
                .WithDescription("Creates a new user account (requires authentication)")
                .WithSecurity("Bearer")
                .WithSecurity("ApiKey")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json<CreateUserRequest>("User creation data", true))
                .WithResponse(201, OpenApiResponseMetadata.Json<User>("Created user"))
                .WithResponse(400, OpenApiResponseMetadata.BadRequest())
                .WithResponse(401, OpenApiResponseMetadata.Unauthorized())
                .WithResponse(409, OpenApiResponseMetadata.Conflict()),
            requireAuthentication: true);

            app.Rest.Delete("/users/{id}", async (req) =>
            {
                string id = req.Parameters["id"];
                User user = _Users.FirstOrDefault(u => u.Id == id);

                if (user == null)
                    throw new SwiftStackException(ApiResultEnum.NotFound, "User not found");

                _Users.Remove(user);

                req.Http.Response.StatusCode = 204;
                return null;
            },
            api => api
                .WithTag("Users")
                .WithSummary("Delete a user")
                .WithDescription("Deletes a user by their ID (requires authentication)")
                .WithParameter(OpenApiParameterMetadata.Path("id", "The user ID"))
                .WithSecurity("Bearer")
                .WithSecurity("ApiKey")
                .WithResponse(204, OpenApiResponseMetadata.NoContent())
                .WithResponse(401, OpenApiResponseMetadata.Unauthorized())
                .WithResponse(404, OpenApiResponseMetadata.NotFound()),
            requireAuthentication: true);

            #endregion

            app.Rest.WebserverSettings.Hostname = "localhost";
            app.Rest.WebserverSettings.Port = 8000;

            Task rest = Task.Run(() => app.Rest.Run());

            Console.WriteLine("===========================================");
            Console.WriteLine("  SwiftStack OpenAPI Demo Application");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("REST API running on http://localhost:8000");
            Console.WriteLine();
            Console.WriteLine("OpenAPI Documentation:");
            Console.WriteLine("  - OpenAPI JSON: http://localhost:8000/openapi.json");
            Console.WriteLine("  - Swagger UI:   http://localhost:8000/swagger");
            Console.WriteLine();
            Console.WriteLine("Available Endpoints:");
            Console.WriteLine("  Health:");
            Console.WriteLine("    GET  /              - API info");
            Console.WriteLine("    GET  /health        - Health check");
            Console.WriteLine();
            Console.WriteLine("  Products (unauthenticated):");
            Console.WriteLine("    GET  /products           - List products");
            Console.WriteLine("    GET  /products/{id}      - Get product");
            Console.WriteLine("    POST /products           - Create product");
            Console.WriteLine("    PUT  /products/{id}      - Update product");
            Console.WriteLine("    DELETE /products/{id}    - Delete product");
            Console.WriteLine("    GET  /products/search    - Search products");
            Console.WriteLine();
            Console.WriteLine("  Users (authenticated):");
            Console.WriteLine("    GET  /users              - List users");
            Console.WriteLine("    GET  /users/{id}         - Get user");
            Console.WriteLine("    POST /users              - Create user");
            Console.WriteLine("    DELETE /users/{id}       - Delete user");
            Console.WriteLine();
            Console.WriteLine("Authentication:");
            Console.WriteLine("  - Bearer token: 'demo-token'");
            Console.WriteLine("  - API key header (X-API-Key): 'demo-api-key'");
            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }

        private static async Task<AuthResult> AuthenticationRoute(WatsonWebserver.Core.HttpContextBase ctx)
        {
            // Check for Bearer token
            if (!string.IsNullOrEmpty(ctx.Request.Authorization?.BearerToken))
            {
                if (ctx.Request.Authorization.BearerToken == "demo-token")
                {
                    ctx.Metadata = new { Method = "Bearer", Authorized = true };
                    return new AuthResult
                    {
                        AuthenticationResult = AuthenticationResultEnum.Success,
                        AuthorizationResult = AuthorizationResultEnum.Permitted
                    };
                }
            }

            // Check for API key header
            string apiKey = ctx.Request.Headers["X-API-Key"];
            if (!string.IsNullOrEmpty(apiKey) && apiKey == "demo-api-key")
            {
                ctx.Metadata = new { Method = "ApiKey", Authorized = true };
                return new AuthResult
                {
                    AuthenticationResult = AuthenticationResultEnum.Success,
                    AuthorizationResult = AuthorizationResultEnum.Permitted
                };
            }

            return new AuthResult
            {
                AuthenticationResult = AuthenticationResultEnum.NotFound,
                AuthorizationResult = AuthorizationResultEnum.DeniedImplicit
            };
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }

    #region Models

    public class Product
    {
        public string Id { get; set; } = null;
        public string Name { get; set; } = null;
        public string Description { get; set; } = null;
        public decimal Price { get; set; } = 0;
        public string Category { get; set; } = null;
        public bool InStock { get; set; } = true;
    }

    public class User
    {
        public string Id { get; set; } = null;
        public string Email { get; set; } = null;
        public string Name { get; set; } = null;
        public string Role { get; set; } = null;
    }

    public class CreateUserRequest
    {
        public string Email { get; set; } = null;
        public string Name { get; set; } = null;
        public string Role { get; set; } = null;
    }

    #endregion
}
