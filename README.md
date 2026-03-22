![alt tag](https://github.com/jchristn/swiftstack/blob/main/Assets/icon.ico?raw=true)

# SwiftStack

[![NuGet Version](https://img.shields.io/nuget/v/SwiftStack.svg?style=flat)](https://www.nuget.org/packages/SwiftStack/) [![NuGet](https://img.shields.io/nuget/dt/SwiftStack.svg)](https://www.nuget.org/packages/SwiftStack) 

SwiftStack is an opinionated and easy way to build distributed systems — RESTful, message queue–oriented, or WebSocket–based — inspired by the elegant model shown in FastAPI (Python) but designed for C# developers who value clarity and speed.

MIT Licensed • No ceremony • Just build.

---

## ✨ New in v0.4.8

- **Middleware pipeline** for composable request processing
- **Request timeout support** with automatic 408 responses
- **Health check endpoints** with Healthy/Degraded/Unhealthy status
- **Default route support** for custom catch-all handling of unmatched requests
- **OpenAPI 3.0 and Swagger UI support** for REST APIs

---

## 🚀 Simple REST Example

<details>
<summary>Click to expand</summary>

```csharp
using SwiftStack;

class Program
{
    static async Task Main(string[] args)
    {
        SwiftStackApp app = new SwiftStackApp("My test application");

        // Use app.Rest.Route to add a route by specifying the HTTP method as a string
        app.Rest.Route("GET", "/", async (req) => "Hello world");

        // Use app.Rest.Get to add a GET route with query parameter handling
        app.Rest.Get("/search", async (req) =>
        {
            string query = req.Query["q"];
            if (string.IsNullOrEmpty(query)) query = "no query provided";
            int page = int.TryParse(req.Query["page"] as string, out int p) ? p : 1;

            return new
            {
                Query = query,
                Page = page,
                Message = $"Searching for '{query}' on page {page}"
            };
        });

        // Use app.Rest.Post<T> to add a POST route with automatic body deserialization
        app.Rest.Post<User>("/users", async (req) =>
        {
            User user = req.GetData<User>();
            return new { Id = Guid.NewGuid(), user.Email };
        });

        // Use app.Rest.Post (non-generic) to add a POST route without body deserialization;
        // access the raw request body through req.Http.Request
        app.Rest.Post("/upload", async (req) =>
        {
            string rawBody = req.Http.Request.DataAsString;
            return new { Received = rawBody };
        });

        await app.Rest.Run();
    }
}

public class User
{
    public string Email { get; set; }
    public string Name { get; set; }
}
```
</details>

---

## 🔧 REST Routes Without Body Deserialization

<details>
<summary>Click to expand</summary>

By default, `Post<T>`, `Put<T>`, and `Patch<T>` automatically deserialize the request body into type `T`. If you want to handle the raw request body yourself — for example, when receiving binary data, form data, or content you want to parse manually — use the non-generic overloads instead. The request body is accessible directly through `HttpContextBase`.

```csharp
using SwiftStack;

class Program
{
    static async Task Main(string[] args)
    {
        SwiftStackApp app = new SwiftStackApp("Raw body example");

        // POST without automatic deserialization
        app.Rest.Post("/upload", async (req) =>
        {
            string rawBody = req.Http.Request.DataAsString;
            byte[] rawBytes = req.Http.Request.Data;
            return new { Length = rawBytes.Length, ContentType = req.Http.Request.ContentType };
        });

        // PUT without automatic deserialization
        app.Rest.Put("/upload/{id}", async (req) =>
        {
            string id = req.Parameters["id"];
            string rawBody = req.Http.Request.DataAsString;
            return new { Id = id, Body = rawBody };
        });

        // PATCH without automatic deserialization
        app.Rest.Patch("/upload/{id}", async (req) =>
        {
            string id = req.Parameters["id"];
            string rawBody = req.Http.Request.DataAsString;
            return new { Id = id, Body = rawBody };
        });

        // DELETE also supports both patterns
        app.Rest.Delete("/upload/{id}", async (req) => null);

        await app.Rest.Run();
    }
}
```

All non-generic overloads also support OpenAPI documentation:

```csharp
app.Rest.Post("/upload", async (req) =>
{
    string rawBody = req.Http.Request.DataAsString;
    return new { Body = rawBody };
},
api => api
    .WithTag("Uploads")
    .WithSummary("Upload raw content")
    .WithDescription("Accepts raw request body without automatic deserialization"));
```

</details>

---

## 🔐 REST Example with Authentication

<details>
<summary>Click to expand</summary>

```csharp
using SwiftStack;

class Program
{
    static async Task Main(string[] args)
    {
        SwiftStackApp app = new SwiftStackApp("My secure app");
        app.Rest.AuthenticationRoute = AuthenticationRoute;

        app.Rest.Route("GET", "/authenticated", async (req) => "Hello, authenticated user", true);

        await app.Rest.Run();
    }

    static async Task<AuthResult> AuthenticationRoute(HttpContextBase ctx)
    {

        if (ctx.Request.Authorization?.Username == "user" &&
            ctx.Request.Authorization?.Password == "password")
        {
            ctx.Metadata = new { Authorized = true };                    
            return new AuthResult
            {
                AuthenticationResult = AuthenticationResultEnum.Success,
                AuthorizationResult = AuthorizationResultEnum.Permitted
            };
        }
        else
        {
            return new AuthResult
            {
                AuthenticationResult = AuthenticationResultEnum.NotFound,
                AuthorizationResult = AuthorizationResultEnum.DeniedImplicit
            };
        }
    }
}
```
</details>

---

## 🎯 REST with Default/Catch-All Route

<details>
<summary>Click to expand</summary>

You can set a custom default route to handle requests that don't match any registered routes. This is useful for:
- Serving a single-page application (SPA) where all unmatched routes should return the index page
- Implementing custom 404 handling with logging or analytics
- Proxying unmatched requests to another service
- Returning helpful error messages with available endpoints

```csharp
using SwiftStack;
using WatsonWebserver.Core;

class Program
{
    static async Task Main(string[] args)
    {
        SwiftStackApp app = new SwiftStackApp("My app with catch-all");

        // Register your specific routes
        app.Rest.Get("/api/users", async (req) => new[] { "Alice", "Bob" });
        app.Rest.Get("/api/health", async (req) => new { Status = "OK" });

        // Set a custom default route for unmatched requests
        app.Rest.DefaultRoute = async (HttpContextBase ctx) =>
        {
            // Example: Return a custom 404 response
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send("{\"error\": \"Not Found\", \"message\": \"The requested endpoint does not exist.\"}");
        };

        await app.Rest.Run();
    }
}
```

### SPA Fallback Example

For single-page applications, you typically want to serve the index page for any route that doesn't match an API endpoint:

```csharp
app.Rest.DefaultRoute = async (HttpContextBase ctx) =>
{
    // Skip API routes - let them 404 normally
    if (ctx.Request.Url.RawWithoutQuery.StartsWith("/api/"))
    {
        ctx.Response.StatusCode = 404;
        await ctx.Response.Send("{\"error\": \"API endpoint not found\"}");
        return;
    }

    // Serve index.html for all other routes (SPA fallback)
    ctx.Response.ContentType = "text/html";
    string indexContent = File.ReadAllText("./wwwroot/index.html");
    await ctx.Response.Send(indexContent);
};
```

When `DefaultRoute` is not set, the built-in handler returns a `400 Bad Request` response for unmatched requests.

</details>

---

## 📖 REST with OpenAPI and Swagger UI

<details>
<summary>Click to expand</summary>

SwiftStack includes built-in **OpenAPI 3.0** documentation and **Swagger UI** for your REST APIs.

### Basic Setup

```csharp
using SwiftStack;
using SwiftStack.Rest.OpenApi;

SwiftStackApp app = new SwiftStackApp("My API");

// Enable OpenAPI with configuration
app.Rest.UseOpenApi(openApi =>
{
    openApi.Info.Title = "My API";
    openApi.Info.Version = "1.0.0";
    openApi.Info.Description = "A sample API built with SwiftStack";
    openApi.Info.Contact = new OpenApiContact("Support", "support@example.com");

    // Define tags for grouping endpoints
    openApi.Tags.Add(new OpenApiTag("Users", "User management endpoints"));
    openApi.Tags.Add(new OpenApiTag("Products", "Product catalog endpoints"));

    // Define security schemes
    openApi.SecuritySchemes["Bearer"] = OpenApiSecurityScheme.Bearer(
        "JWT",
        "Enter your JWT token");
    openApi.SecuritySchemes["Basic"] = OpenApiSecurityScheme.Basic(
        "Use username:password");
});

await app.Rest.Run();
```

This automatically creates:
- **OpenAPI JSON** at `/openapi.json`
- **Swagger UI** at `/swagger`

### Documenting Routes

Use the fluent API to add metadata to your routes:

```csharp
// Simple route with documentation
app.Rest.Get("/", async (req) => "Hello, World!",
    api => api
        .WithTag("General")
        .WithSummary("Root endpoint")
        .WithDescription("Returns a simple greeting message"));

// Route with path parameters
app.Rest.Get("/users/{id}", async (req) =>
{
    string id = req.Parameters["id"];
    return new { Id = id, Name = "John Doe" };
},
api => api
    .WithTag("Users")
    .WithSummary("Get user by ID")
    .WithParameter(OpenApiParameterMetadata.Path("id", "The user ID"))
    .WithResponse(200, OpenApiResponseMetadata.Json<User>("User details"))
    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

// Route with query parameters
app.Rest.Get("/search", async (req) =>
{
    string query = req.Query["q"];
    int page = int.TryParse(req.Query["page"] as string, out int p) ? p : 1;
    return new { Query = query, Page = page };
},
api => api
    .WithTag("General")
    .WithSummary("Search endpoint")
    .WithParameter(OpenApiParameterMetadata.Query("q", "Search query", true))
    .WithParameter(OpenApiParameterMetadata.Query("page", "Page number", false,
        OpenApiSchemaMetadata.Integer())));

// Route with request body (type is inferred from generic parameter)
app.Rest.Post<User>("/users", async (req) =>
{
    User user = req.GetData<User>();
    return new { Id = Guid.NewGuid(), user.Email };
},
api => api
    .WithTag("Users")
    .WithSummary("Create a new user")
    .WithRequestBody(OpenApiRequestBodyMetadata.Json<User>("User to create", true))
    .WithResponse(201, OpenApiResponseMetadata.Json<User>("Created user")));

// Authenticated route
app.Rest.Get("/profile", async (req) => new { Email = "user@example.com" },
    api => api
        .WithTag("Users")
        .WithSummary("Get current user profile")
        .WithSecurity("Bearer")
        .WithResponse(200, OpenApiResponseMetadata.Json<User>("User profile"))
        .WithResponse(401, OpenApiResponseMetadata.Unauthorized()),
    requireAuthentication: true);
```

### Schema Generation

Schemas are automatically generated from your C# types:

```csharp
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Use in responses - schema is auto-generated via reflection
.WithResponse(200, OpenApiResponseMetadata.Json<User>("User data"))

// Or create schemas manually
OpenApiSchemaMetadata.String()                    // string
OpenApiSchemaMetadata.Integer()                   // int32
OpenApiSchemaMetadata.Long()                      // int64
OpenApiSchemaMetadata.Number()                    // double
OpenApiSchemaMetadata.Boolean()                   // boolean
OpenApiSchemaMetadata.Array(OpenApiSchemaMetadata.String())  // string[]
OpenApiSchemaMetadata.FromType<MyClass>()         // complex object
```

### Common Response Helpers

```csharp
OpenApiResponseMetadata.Json<T>(description)      // 200 with JSON body
OpenApiResponseMetadata.Text(description)         // 200 with text body
OpenApiResponseMetadata.NoContent()               // 204 No Content
OpenApiResponseMetadata.BadRequest()              // 400 Bad Request
OpenApiResponseMetadata.Unauthorized()            // 401 Unauthorized
OpenApiResponseMetadata.NotFound()                // 404 Not Found
OpenApiResponseMetadata.Conflict()                // 409 Conflict
OpenApiResponseMetadata.InternalServerError()     // 500 Internal Server Error
```

### Enabling and Disabling OpenAPI/Swagger

You can control whether OpenAPI and Swagger UI endpoints are exposed:

```csharp
// Both OpenAPI and Swagger UI enabled (default)
app.Rest.UseOpenApi();

// Disable Swagger UI but keep OpenAPI JSON endpoint
app.Rest.UseOpenApi(openApi =>
{
    openApi.EnableSwaggerUi = false;
});

// Disable both OpenAPI and Swagger UI entirely
app.Rest.UseOpenApi(openApi =>
{
    openApi.EnableOpenApi = false;
});
```

| Setting | Default | Effect |
|---------|---------|--------|
| `EnableOpenApi` | `true` | When `false`, disables `/openapi.json` endpoint and Swagger UI |
| `EnableSwaggerUi` | `true` | When `false`, disables `/swagger` endpoint only |

> **Note:** Swagger UI depends on the OpenAPI document, so setting `EnableOpenApi = false` will disable Swagger UI regardless of the `EnableSwaggerUi` setting.

### Configuration Options

```csharp
app.Rest.UseOpenApi(openApi =>
{
    // Enable/disable endpoints (defaults shown)
    openApi.EnableOpenApi = true;      // Set to false to disable OpenAPI entirely
    openApi.EnableSwaggerUi = true;    // Set to false to disable Swagger UI only

    // Customize paths (defaults shown)
    openApi.DocumentPath = "/openapi.json";
    openApi.SwaggerUiPath = "/swagger";

    // API info
    openApi.Info.Title = "My API";
    openApi.Info.Version = "1.0.0";
    openApi.Info.Description = "API description";
    openApi.Info.TermsOfService = "https://example.com/terms";
    openApi.Info.Contact = new OpenApiContact("Name", "email@example.com");
    openApi.Info.License = new OpenApiLicense("MIT", "https://opensource.org/licenses/MIT");

    // External docs
    openApi.ExternalDocs = new OpenApiExternalDocs(
        "https://docs.example.com",
        "Full documentation");

    // Server configuration
    openApi.Servers.Add(new OpenApiServer("https://api.example.com", "Production"));
    openApi.Servers.Add(new OpenApiServer("https://staging-api.example.com", "Staging"));
});
```

</details>

---

## 🔗 Middleware Pipeline

<details>
<summary>Click to expand</summary>

SwiftStack supports composable middleware that wraps route handlers. Middleware executes in registration order and can inspect/modify requests, short-circuit the pipeline, or run logic after the handler completes.

```csharp
using SwiftStack;
using SwiftStack.Rest.Middleware;

SwiftStackApp app = new SwiftStackApp("Middleware Example");

// Logging middleware
app.Rest.Use(async (ctx, next, token) =>
{
    Console.WriteLine($"Request: {ctx.Request.Method} {ctx.Request.Url.RawWithoutQuery}");
    await next();
    Console.WriteLine($"Response: {ctx.Response.StatusCode}");
});

// Auth middleware that short-circuits unauthorized requests
app.Rest.Use(async (ctx, next, token) =>
{
    if (ctx.Request.Url.RawWithoutQuery.StartsWith("/admin") &&
        ctx.Request.Headers["X-Api-Key"] != "secret")
    {
        ctx.Response.StatusCode = 403;
        await ctx.Response.Send("{\"error\": \"Forbidden\"}");
        return; // short-circuit — don't call next()
    }
    await next();
});

app.Rest.Get("/", async (req) => "Hello World");
app.Rest.Get("/admin/stats", async (req) => new { Users = 42 });

await app.Rest.Run();
```

Middleware must be registered before calling `Run()`. Call `next()` to continue the pipeline, or return without calling `next()` to short-circuit.

</details>

---

## ⏱ Request Timeouts

<details>
<summary>Click to expand</summary>

Enable automatic request timeouts that return HTTP 408 when a handler exceeds the configured duration. The cancellation token is available to route handlers via `req.CancellationToken` for cooperative cancellation.

```csharp
using SwiftStack;

SwiftStackApp app = new SwiftStackApp("Timeout Example");

// Set a 30-second timeout for all requests
app.Rest.UseTimeout(TimeSpan.FromSeconds(30));

// Fast handler — completes normally
app.Rest.Get("/fast", async (req) => new { Result = "OK" });

// Slow handler — uses the cancellation token for cooperative cancellation
app.Rest.Get("/slow", async (req) =>
{
    await Task.Delay(60000, req.CancellationToken); // will be cancelled after 30s
    return new { Result = "done" };
});

await app.Rest.Run();
```

When a request times out, the client receives:
```json
{
    "Error": "RequestTimeout",
    "Description": "The request timed out.",
    "Message": "The request timed out."
}
```

</details>

---

## 🏥 Health Check Endpoints

<details>
<summary>Click to expand</summary>

Add health check endpoints that report application status. Supports default, custom, and multiple health check paths.

```csharp
using SwiftStack;
using SwiftStack.Rest.Health;

SwiftStackApp app = new SwiftStackApp("Health Check Example");

// Default health check at /health — returns {"Status": "Healthy"}
app.Rest.UseHealthCheck();

// Custom health check with data
app.Rest.UseHealthCheck(settings =>
{
    settings.Path = "/healthz";
    settings.CustomCheck = async (token) =>
    {
        bool dbOk = await CheckDatabase(token);
        return new HealthCheckResult
        {
            Status = dbOk ? HealthStatusEnum.Healthy : HealthStatusEnum.Unhealthy,
            Description = dbOk ? "All systems operational" : "Database unavailable",
            Data = new Dictionary<string, object>
            {
                { "database", dbOk ? "ok" : "down" },
                { "uptime", Environment.TickCount64 / 1000 }
            }
        };
    };
});

await app.Rest.Run();
```

| Status | HTTP Code | Description |
|--------|-----------|-------------|
| `Healthy` | 200 | Application is operating normally |
| `Degraded` | 200 | Operational but with reduced performance |
| `Unhealthy` | 503 | Unable to serve requests properly |

</details>

---

## 📨 Simple RabbitMQ Example

<details>
<summary>Click to expand</summary>

SwiftStack includes **first-class RabbitMQ support**, including *resilient* producer/consumer and broadcaster/receiver patterns.  
Resilient modes use on-disk index files to recover state across process restarts.

```csharp
using SwiftStack;
using SwiftStack.RabbitMq;

// Initialize app and RabbitMQ integration
SwiftStackApp app = new SwiftStackApp("RabbitMQ Example");
RabbitMqApp rabbit = new RabbitMqApp(app);

// Define queue settings
QueueProperties queueProps = new QueueProperties
{
    Hostname = "localhost",
    Name = "demo-queue",
    AutoDelete = true
};

// Create producer and consumer
var producer = new RabbitMqProducer<string>(app.Logging, queueProps, 1024 * 1024);
var consumer = new RabbitMqConsumer<string>(app.Logging, queueProps, true);

consumer.MessageReceived += (sender, e) =>
{
    Console.WriteLine($"[Consumer] {e.Data}");
};

// Initialize and send
await producer.InitializeAsync();
await consumer.InitializeAsync();

for (int i = 1; i <= 5; i++)
{
    await producer.SendMessage($"Message {i}", Guid.NewGuid().ToString());
    await Task.Delay(500);
}
```

**Resilient** versions are identical except you use:

```csharp
var producer = new ResilientRabbitMqProducer<string>(app.Logging, queueProps, "./producer.idx", 1024 * 1024);
var consumer = new ResilientRabbitMqConsumer<string>(app.Logging, queueProps, "./consumer.idx", 4, true);
```

and the same for broadcaster/receiver via:

```csharp
var broadcaster = new RabbitMqBroadcaster<MyType>(...);
var receiver = new RabbitMqBroadcastReceiver<MyType>(...);
```
</details>

---

## 🔌Simple WebSockets Example

<details>
<summary>Click to expand</summary>

SwiftStack makes it trivial to stand up **WebSocket servers** with routing, default handlers, and direct server→client messaging.

```csharp
using SwiftStack;
using SwiftStack.Websockets;

SwiftStackApp app = new SwiftStackApp("WebSockets Demo");
WebsocketsApp wsApp = new WebsocketsApp(app);

// Route for "echo"
wsApp.AddRoute("echo", async (msg, token) =>
{
    await msg.RespondAsync($"Echo: {msg.DataAsString}");
});

// Default route
wsApp.DefaultRoute = async (msg, token) =>
{
    await msg.RespondAsync("No route matched, sorry!");
};

// Start server
app.LoggingSettings.EnableConsole = true;
Task serverTask = wsApp.Run("127.0.0.1", 9006, CancellationToken.None);

// Example: sending server→client message after connect
wsApp.ClientConnected += async (sender, client) =>
{
    await wsApp.WebsocketServer.SendAsync(client.Guid, "Welcome to the server!");
};

await serverTask;
```

**Client** (any WebSocket library works — here’s with `System.Net.WebSockets`):

```csharp
using var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://127.0.0.1:9006/echo"), CancellationToken.None);
await ws.SendAsync(Encoding.UTF8.GetBytes("Hello"), WebSocketMessageType.Text, true, CancellationToken.None);
```
</details>

---

## 📦 Installation

```bash
dotnet add package SwiftStack
```

---

## 📜 Version History

See [CHANGELOG.md](CHANGELOG.md) for details.

---

## ❤️ Donations

If you’d like to financially support development: see [DONATIONS.md](DONATIONS.md).

---

## 🖼 Logo

Thanks to [pngall.com](https://www.pngall.com/fast-png/download/92775/) for the lightning icon.

---
