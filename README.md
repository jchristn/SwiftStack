![alt tag](https://github.com/jchristn/swiftstack/blob/main/Assets/icon.ico?raw=true)

# SwiftStack

[![NuGet Version](https://img.shields.io/nuget/v/SwiftStack.svg?style=flat)](https://www.nuget.org/packages/SwiftStack/) [![NuGet](https://img.shields.io/nuget/dt/SwiftStack.svg)](https://www.nuget.org/packages/SwiftStack) 

SwiftStack is an opinionated and easy way to build distributed systems — RESTful, message queue–oriented, or WebSocket–based — inspired by the elegant model shown in FastAPI (Python) but designed for C# developers who value clarity and speed.

MIT Licensed • No ceremony • Just build.

---

## ✨ New in v0.4.x

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

        app.Rest.Route("GET", "/", async (req) => "Hello world");

        app.Rest.Route("POST", "/loopback", async (req) => req.Data);

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

        await app.Rest.Run();
    }
}
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
