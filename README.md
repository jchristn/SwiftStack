![alt tag](https://github.com/jchristn/swiftstack/blob/main/Assets/icon.ico?raw=true)

# SwiftStack

[![NuGet Version](https://img.shields.io/nuget/v/SwiftStack.svg?style=flat)](https://www.nuget.org/packages/SwiftStack/) [![NuGet](https://img.shields.io/nuget/dt/SwiftStack.svg)](https://www.nuget.org/packages/SwiftStack) 

SwiftStack is an opinionated and easy way to build distributed systems — RESTful, message queue–oriented, or WebSocket–based — inspired by the elegant model shown in FastAPI (Python) but designed for C# developers who value clarity and speed.

MIT Licensed • No ceremony • Just build.

---

## ✨ New in v0.3.x

- **WebSockets application support** via `WebsocketsApp`
- **RabbitMQ resilient interfaces** with on-disk indexing for recovery

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
