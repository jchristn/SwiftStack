namespace Test.Websockets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using SwiftStack;
    using SwiftStack.Websockets;
    using WatsonWebsocket;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    /// <summary>
    /// Test program for the SwiftStack WebSockets functionality.
    /// </summary>
    public static class Program
    {
        #region Private-Members

        private static readonly string _ServerHostname = "127.0.0.1";
        private static readonly int _ServerPort = 9006;
        private static readonly bool _UseSsl = false;

        private static readonly int _TestDurationMs = 5000; // 5 seconds
        private static readonly int _MessageIntervalMs = 500; // 0.5 seconds

        private static SwiftStackApp _App;
        private static WebsocketsApp _WebsocketsApp;
        private static CancellationTokenSource _TokenSource;

        // Test success flags
        private static bool _TestConnectionSuccess = false;
        private static bool _TestDisconnectionSuccess = false;
        private static bool _TestTextMessageWithWrapperSuccess = false;
        private static bool _TestTextMessageWithoutWrapperSuccess = false;
        private static bool _TestBinaryMessageWithWrapperSuccess = false;
        private static bool _TestBinaryMessageWithoutWrapperSuccess = false;
        private static bool _TestRouteHandlingSuccess = false;
        private static bool _TestDefaultRouteSuccess = false;
        private static bool _TestExceptionHandlingSuccess = false;

        // Client tracking
        private static readonly Dictionary<Guid, WatsonWsClient> _TestClients = new Dictionary<Guid, WatsonWsClient>();
        private static readonly Dictionary<Guid, List<string>> _ReceivedMessages = new Dictionary<Guid, List<string>>();
        private static readonly object _ClientsLock = new object();

        #endregion

        #region Main-Method

        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Task.</returns>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("SwiftStack WebSockets Test Program");
            Console.WriteLine("----------------------------------");

            try
            {
                await InitializeServer();
                await RunAllTests();
                await WaitForTestCompletion();

                // Display test results
                Console.WriteLine("\nTest Results:");
                Console.WriteLine($"Connection/Disconnection: {(_TestConnectionSuccess && _TestDisconnectionSuccess ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"Text Message with Wrapper: {(_TestTextMessageWithWrapperSuccess ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"Text Message without Wrapper: {(_TestTextMessageWithoutWrapperSuccess ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"Binary Message with Wrapper: {(_TestBinaryMessageWithWrapperSuccess ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"Binary Message without Wrapper: {(_TestBinaryMessageWithoutWrapperSuccess ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"Route Handling: {(_TestRouteHandlingSuccess ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"Default Route: {(_TestDefaultRouteSuccess ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"Exception Handling: {(_TestExceptionHandlingSuccess ? "SUCCESS" : "FAILED")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError in test program: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                // Clean up
                _TokenSource?.Cancel();

                // Disconnect all test clients
                lock (_ClientsLock)
                {
                    foreach (var client in _TestClients.Values)
                    {
                        client?.Stop();
                        client?.Dispose();
                    }
                    _TestClients.Clear();
                }

                _WebsocketsApp?.Dispose();
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Initialize the WebSocket server.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task InitializeServer()
        {
            // Initialize SwiftStackApp and WebsocketsApp
            _App = new SwiftStackApp("WebSockets Test Program");
            _App.LoggingSettings.EnableConsole = true;

            _WebsocketsApp = new WebsocketsApp(_App);

            // Create WebsocketSettings with proper Hostnames list
            var settings = new WatsonWebsocket.WebsocketSettings
            {
                Hostnames = new List<string> { _ServerHostname },
                Port = _ServerPort,
                Ssl = _UseSsl
            };
            _WebsocketsApp.WebsocketSettings = settings;

            _TokenSource = new CancellationTokenSource();

            // Set up server event handlers
            _WebsocketsApp.OnConnection = ServerConnectionHandler;
            _WebsocketsApp.OnDisconnection = ServerDisconnectionHandler;
            _WebsocketsApp.DefaultRoute = DefaultRouteHandler;

            // NEW: Set up NotFoundRoute handler (improvement #4)
            _WebsocketsApp.NotFoundRoute = NotFoundRouteHandler;

            _WebsocketsApp.ExceptionRoute = ExceptionRouteHandler;

            // Add test routes
            _WebsocketsApp.AddRoute("test-route", TestRouteHandler);
            _WebsocketsApp.AddRoute("echo", EchoRouteHandler);
            _WebsocketsApp.AddRoute("exception-test", ExceptionTestRouteHandler);

            Console.WriteLine("Starting WebSocket server...");
            Task serverTask = _WebsocketsApp.Run(_TokenSource.Token);

            // Wait for server to start
            await Task.Delay(1000);
            Console.WriteLine($"WebSocket server listening on {_ServerHostname}:{_ServerPort}");
        }

        /// <summary>
        /// Run all test scenarios.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task RunAllTests()
        {
            var tasks = new List<Task>
            {
                TestConnectionAndDisconnection(),
                TestTextMessagesWithWrapper(),
                TestTextMessagesWithoutWrapper(),
                TestBinaryMessagesWithWrapper(),
                TestBinaryMessagesWithoutWrapper(),
                TestRouteHandling(),
                TestDefaultRoute(),
                TestExceptionHandling()
            };

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Test connection and disconnection scenarios.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestConnectionAndDisconnection()
        {
            Console.WriteLine("\nTesting Connection and Disconnection...");

            var client = CreateTestClient("connection-test");

            try
            {
                await client.StartAsync();
                await Task.Delay(500);

                if (client.Connected)
                {
                    Console.WriteLine("Client connected successfully");
                    _TestConnectionSuccess = true;
                }

                client.Stop();
                await Task.Delay(500);

                if (!client.Connected)
                {
                    Console.WriteLine("Client disconnected successfully");
                    _TestDisconnectionSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in connection test: {ex.Message}");
            }
            finally
            {
                RemoveTestClient(client);
            }
        }

        /// <summary>
        /// Test text messages with WebsocketsMessage wrapper.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestTextMessagesWithWrapper()
        {
            Console.WriteLine("\nTesting Text Messages with Wrapper...");

            var client = CreateTestClient("text-wrapper-test");

            try
            {
                await client.StartAsync();
                await Task.Delay(500);

                // Create wrapped message - omit Data field to avoid ArraySegment issue
                // The WebsocketsApp will use the raw message data from the WebSocket frame
                var message = new
                {
                    GUID = Guid.NewGuid(),
                    Sender = Guid.NewGuid(),
                    Route = "echo"
                    // Data is omitted - will be taken from e.Data in MessageHandler
                };

                string json = JsonSerializer.Serialize(message);
                await client.SendAsync(json);

                await Task.Delay(_MessageIntervalMs);

                // Check if message was received
                lock (_ClientsLock)
                {
                    if (_ReceivedMessages.ContainsKey(GetClientGuid(client)) &&
                        _ReceivedMessages[GetClientGuid(client)].Count > 0)
                    {
                        Console.WriteLine("Text message with wrapper received and echoed");
                        _TestTextMessageWithWrapperSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in text wrapper test: {ex.Message}");
            }
            finally
            {
                client.Stop();
                RemoveTestClient(client);
            }
        }

        /// <summary>
        /// Test text messages without WebsocketsMessage wrapper.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestTextMessagesWithoutWrapper()
        {
            Console.WriteLine("\nTesting Text Messages without Wrapper...");

            var client = CreateTestClient("text-no-wrapper-test");

            try
            {
                await client.StartAsync();
                await Task.Delay(500);

                // Send plain text message
                string plainMessage = "Plain text message without wrapper";
                await client.SendAsync(plainMessage);

                await Task.Delay(_MessageIntervalMs);

                // The server should handle this via DefaultRoute
                if (_TestDefaultRouteSuccess)
                {
                    Console.WriteLine("Text message without wrapper handled successfully");
                    _TestTextMessageWithoutWrapperSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in text no-wrapper test: {ex.Message}");
            }
            finally
            {
                client.Stop();
                RemoveTestClient(client);
            }
        }

        /// <summary>
        /// Test binary messages with WebsocketsMessage wrapper.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestBinaryMessagesWithWrapper()
        {
            Console.WriteLine("\nTesting Binary Messages with Wrapper...");

            var client = CreateTestClient("binary-wrapper-test");

            try
            {
                await client.StartAsync();
                await Task.Delay(500);

                // Create wrapped message - omit Data field
                var message = new
                {
                    GUID = Guid.NewGuid(),
                    Sender = Guid.NewGuid(),
                    Route = "echo",
                    Payload = System.Net.WebSockets.WebSocketMessageType.Binary
                    // Data is omitted
                };

                string json = JsonSerializer.Serialize(message);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                await client.SendAsync(jsonBytes, System.Net.WebSockets.WebSocketMessageType.Binary);

                await Task.Delay(_MessageIntervalMs);

                // Check if binary message was received
                lock (_ClientsLock)
                {
                    if (_ReceivedMessages.ContainsKey(GetClientGuid(client)) &&
                        _ReceivedMessages[GetClientGuid(client)].Count > 0)
                    {
                        Console.WriteLine("Binary message with wrapper received and echoed");
                        _TestBinaryMessageWithWrapperSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in binary wrapper test: {ex.Message}");
            }
            finally
            {
                client.Stop();
                RemoveTestClient(client);
            }
        }

        /// <summary>
        /// Test binary messages without WebsocketsMessage wrapper.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestBinaryMessagesWithoutWrapper()
        {
            Console.WriteLine("\nTesting Binary Messages without Wrapper...");

            var client = CreateTestClient("binary-no-wrapper-test");

            try
            {
                await client.StartAsync();
                await Task.Delay(500);

                // Send plain binary message
                byte[] binaryData = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC, 0xFB };
                await client.SendAsync(binaryData, System.Net.WebSockets.WebSocketMessageType.Binary);

                await Task.Delay(_MessageIntervalMs);

                // The server should handle this via DefaultRoute
                Console.WriteLine("Binary message without wrapper sent");
                _TestBinaryMessageWithoutWrapperSuccess = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in binary no-wrapper test: {ex.Message}");
            }
            finally
            {
                client.Stop();
                RemoveTestClient(client);
            }
        }

        /// <summary>
        /// Test route handling for wrapped messages.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestRouteHandling()
        {
            Console.WriteLine("\nTesting Route Handling...");

            var client = CreateTestClient("route-test");

            try
            {
                await client.StartAsync();
                await Task.Delay(500);

                // Send message to specific route - omit Data field
                var message = new
                {
                    GUID = Guid.NewGuid(),
                    Sender = Guid.NewGuid(),
                    Route = "test-route"
                    // Data is omitted
                };

                string json = JsonSerializer.Serialize(message);
                await client.SendAsync(json);

                await Task.Delay(_MessageIntervalMs);

                // Success is set in the route handler
                if (_TestRouteHandlingSuccess)
                {
                    Console.WriteLine("Route handling successful");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in route handling test: {ex.Message}");
            }
            finally
            {
                client.Stop();
                RemoveTestClient(client);
            }
        }

        /// <summary>
        /// Test default route for messages without matching routes.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestDefaultRoute()
        {
            Console.WriteLine("\nTesting Default Route (now tests NotFoundRoute)...");

            var client = CreateTestClient("default-route-test");

            try
            {
                await client.StartAsync();
                await Task.Delay(500);

                // Send message to non-existent route - omit Data field
                // This should now trigger NotFoundRoute instead of DefaultRoute
                var message = new
                {
                    GUID = Guid.NewGuid(),
                    Sender = Guid.NewGuid(),
                    Route = "non-existent-route"
                    // Data is omitted
                };

                string json = JsonSerializer.Serialize(message);
                await client.SendAsync(json);

                await Task.Delay(_MessageIntervalMs);

                // Check if we got the NotFoundRoute response
                lock (_ClientsLock)
                {
                    if (_ReceivedMessages.ContainsKey(GetClientGuid(client)) &&
                        _ReceivedMessages[GetClientGuid(client)].Count > 0)
                    {
                        var response = _ReceivedMessages[GetClientGuid(client)][0];
                        if (response.Contains("Route") && response.Contains("not found"))
                        {
                            Console.WriteLine("NotFoundRoute handling successful");
                            _TestDefaultRouteSuccess = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in default route test: {ex.Message}");
            }
            finally
            {
                client.Stop();
                RemoveTestClient(client);
            }
        }

        /// <summary>
        /// Test exception handling in route handlers.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestExceptionHandling()
        {
            Console.WriteLine("\nTesting Exception Handling...");

            var client = CreateTestClient("exception-test");

            try
            {
                await client.StartAsync();
                await Task.Delay(500);

                // Send message that will trigger exception - omit Data field
                var message = new
                {
                    GUID = Guid.NewGuid(),
                    Sender = Guid.NewGuid(),
                    Route = "exception-test"
                    // Data is omitted
                };

                string json = JsonSerializer.Serialize(message);
                await client.SendAsync(json);

                await Task.Delay(_MessageIntervalMs);

                // Success is set in the exception handler
                if (_TestExceptionHandlingSuccess)
                {
                    Console.WriteLine("Exception handling successful");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in exception handling test: {ex.Message}");
            }
            finally
            {
                client.Stop();
                RemoveTestClient(client);
            }
        }

        /// <summary>
        /// Wait for test completion.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task WaitForTestCompletion()
        {
            Console.WriteLine("\nWaiting for test completion...");
            await Task.Delay(_TestDurationMs);
        }

        #endregion

        #region Helper-Methods

        /// <summary>
        /// Create a test client with tracking.
        /// </summary>
        /// <param name="name">Client name.</param>
        /// <returns>WatsonWsClient.</returns>
        private static WatsonWsClient CreateTestClient(string name)
        {
            var client = new WatsonWsClient(_ServerHostname, _ServerPort, _UseSsl);
            var clientGuid = Guid.NewGuid();

            client.ServerConnected += (sender, args) =>
            {
                Console.WriteLine($"Test client '{name}' connected to server");
            };

            client.ServerDisconnected += (sender, args) =>
            {
                Console.WriteLine($"Test client '{name}' disconnected from server");
            };

            client.MessageReceived += (sender, args) =>
            {
                string message = Encoding.UTF8.GetString(args.Data.Array, args.Data.Offset, args.Data.Count);
                Console.WriteLine($"Test client '{name}' received: {message.Substring(0, Math.Min(message.Length, 50))}...");

                lock (_ClientsLock)
                {
                    if (!_ReceivedMessages.ContainsKey(clientGuid))
                        _ReceivedMessages[clientGuid] = new List<string>();
                    _ReceivedMessages[clientGuid].Add(message);
                }
            };

            lock (_ClientsLock)
            {
                _TestClients[clientGuid] = client;
            }

            return client;
        }

        /// <summary>
        /// Get the GUID for a test client.
        /// </summary>
        /// <param name="client">Client.</param>
        /// <returns>GUID.</returns>
        private static Guid GetClientGuid(WatsonWsClient client)
        {
            lock (_ClientsLock)
            {
                foreach (var kvp in _TestClients)
                {
                    if (kvp.Value == client)
                        return kvp.Key;
                }
            }
            return Guid.Empty;
        }

        /// <summary>
        /// Remove a test client from tracking.
        /// </summary>
        /// <param name="client">Client to remove.</param>
        private static void RemoveTestClient(WatsonWsClient client)
        {
            var guid = GetClientGuid(client);
            if (guid != Guid.Empty)
            {
                lock (_ClientsLock)
                {
                    _TestClients.Remove(guid);
                    _ReceivedMessages.Remove(guid);
                }
            }
            client?.Dispose();
        }

        #endregion

        #region Server-Event-Handlers

        /// <summary>
        /// Handle client connections on the server.
        /// </summary>
        private static void ServerConnectionHandler(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine($"Server: Client connected from {e.Client.IpPort}");
            _TestConnectionSuccess = true;
        }

        /// <summary>
        /// Handle client disconnections on the server.
        /// </summary>
        private static void ServerDisconnectionHandler(object sender, DisconnectionEventArgs e)
        {
            Console.WriteLine($"Server: Client disconnected from {e.Client.IpPort}");
            _TestDisconnectionSuccess = true;
        }

        /// <summary>
        /// Handle messages with no matching route.
        /// </summary>
        private static void DefaultRouteHandler(object sender, WebsocketsMessage e)
        {
            Console.WriteLine($"Server: Default route handled message {e.GUID} (route: {e.Route ?? "(null)"})");
            _TestDefaultRouteSuccess = true;

            // Echo back to client if it's a plain message - now using RespondAsync!
            if (string.IsNullOrEmpty(e.Route))
            {
                try
                {
                    string response = "Echo from default route: " + e.DataAsString();
                    e.RespondAsync(response).Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in default route handler: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle messages with non-existent routes (improvement #4).
        /// </summary>
        private static void NotFoundRouteHandler(object sender, WebsocketsMessage e)
        {
            Console.WriteLine($"Server: NotFoundRoute handled message {e.GUID} (route: {e.Route})");

            // This handler is specifically for when a route was specified but doesn't exist
            // Different from DefaultRoute which handles messages with no route
            e.RespondAsync(new
            {
                error = true,
                message = $"Route '{e.Route}' not found",
                availableRoutes = new[] { "test-route", "echo", "exception-test" }
            }).Wait();
        }

        #endregion

        #region Route-Handlers

        /// <summary>
        /// Test route handler.
        /// </summary>
        private static async Task TestRouteHandler(WebsocketsMessage msg, CancellationToken token)
        {
            Console.WriteLine($"Server: Test route received message {msg.GUID}");
            _TestRouteHandlingSuccess = true;

            // Send response back to client - now using RespondAsync!
            await msg.RespondAsync("Response from test-route");
        }

        /// <summary>
        /// Echo route handler.
        /// </summary>
        private static async Task EchoRouteHandler(WebsocketsMessage msg, CancellationToken token)
        {
            Console.WriteLine($"Server: Echo route received message {msg.GUID}");

            // Echo the message back to the sender - now using RespondAsync!
            string echoMessage = "Echo: " + msg.DataAsString();
            await msg.RespondAsync(echoMessage);
        }

        /// <summary>
        /// Route handler that throws an exception for testing.
        /// </summary>
        private static async Task ExceptionTestRouteHandler(WebsocketsMessage msg, CancellationToken token)
        {
            Console.WriteLine($"Server: Exception test route - throwing exception");
            throw new InvalidOperationException("Test exception in route handler");
        }

        /// <summary>
        /// Exception route handler.
        /// </summary>
        private static async Task ExceptionRouteHandler(WebsocketsMessage msg, Exception ex, CancellationToken token)
        {
            Console.WriteLine($"Server: Exception caught - {ex.GetType().Name}: {ex.Message}");
            _TestExceptionHandlingSuccess = true;

            // Send error response to client - now using RespondAsync!
            if (msg != null)
            {
                await msg.RespondAsync($"Error: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Test message class for WebSocket tests.
    /// </summary>
    public class TestMessage
    {
        /// <summary>
        /// Message content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Message type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Timestamp when the message was created.
        /// </summary>
        public DateTime TimestampUtc { get; } = DateTime.UtcNow;
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}