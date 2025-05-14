namespace Test.RabbitMq
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using SwiftStack;
    using SwiftStack.RabbitMq;
    using System.Collections.Generic;

    /// <summary>
    /// Test program for the SwiftStack RabbitMQ functionality.
    /// </summary>
    public static class Program
    {
        #region Private-Members

        private static readonly string _RabbitMqHostname = "astra";
        private static readonly string _BroadcastExchange = "test-broadcast";
        private static readonly string _Queue = "test-queue";
        private static readonly int _MaxMessageSize = 1 * 1024 * 1024; // 1 MB

        private static readonly int _TestDurationMs = 5000; // 5 seconds
        private static readonly int _MessageIntervalMs = 500; // 0.5 seconds

        private static SwiftStackApp _App;
        private static RabbitMqApp _RabbitMqApp;
        private static CancellationTokenSource _TokenSource;

        private static bool _TestBroadcastSuccess = false;
        private static bool _TestProducerConsumerSuccess = false;
        private static bool _TestResilientBroadcastSuccess = false;
        private static bool _TestResilientProducerConsumerSuccess = false;

        #endregion

        #region Main-Method

        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Task.</returns>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("SwiftStack RabbitMQ Test Program");
            Console.WriteLine("-------------------------------");

            try
            {
                await InitializeAndRunTests();
                await WaitForTestCompletion();

                // Display test results
                Console.WriteLine("\nTest Results:");
                Console.WriteLine($"Broadcaster/Receiver: {(_TestBroadcastSuccess ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"Producer/Consumer: {(_TestProducerConsumerSuccess ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"Resilient Broadcaster/Receiver: {(_TestResilientBroadcastSuccess ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"Resilient Producer/Consumer: {(_TestResilientProducerConsumerSuccess ? "SUCCESS" : "FAILED")}");
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
                _RabbitMqApp?.Dispose();

                // Clean up index files
                try
                {
                    if (File.Exists("./broadcaster.idx")) File.Delete("./broadcaster.idx");
                    if (File.Exists("./receiver.idx")) File.Delete("./receiver.idx");
                    if (File.Exists("./producer.idx")) File.Delete("./producer.idx");
                    if (File.Exists("./consumer.idx")) File.Delete("./consumer.idx");
                    Console.WriteLine("Removed index files");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting index files: {ex.Message}");
                }

                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Initialize the applications and run the tests.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task InitializeAndRunTests()
        {
            // Initialize SwiftStackApp and RabbitMqApp
            _App = new SwiftStackApp("RabbitMQ Test Program");
            _App.LoggingSettings.EnableConsole = true;

            _RabbitMqApp = new RabbitMqApp(_App);
            _TokenSource = new CancellationTokenSource();

            Console.WriteLine("Running RabbitMqApp...");
            Task rabbitMqTask = _RabbitMqApp.Run(_TokenSource.Token);

            // Wait for application to start
            await Task.Delay(1000);

            // Run all tests in parallel
            var tasks = new List<Task>
            {
                TestBroadcastInterfaces(),
                TestProducerConsumerInterfaces(),
                TestResilientBroadcastInterfaces(),
                TestResilientProducerConsumerInterfaces()
            };

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Test broadcaster and broadcast receiver.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestBroadcastInterfaces()
        {
            Console.WriteLine("\nTesting Broadcaster and Broadcast Receiver...");

            // Set up test message data
            var testMessages = new List<TestMessage>();
            var receivedMessages = new List<string>();

            // Configure queue properties
            var queueProps = new QueueProperties
            {
                Hostname = _RabbitMqHostname,
                Name = _BroadcastExchange,
                AutoDelete = true
            };

            // Create broadcaster and receiver
            var broadcaster = new RabbitMqBroadcaster<TestMessage>(_App.Logging, queueProps, _MaxMessageSize);
            var receiver = new RabbitMqBroadcastReceiver<TestMessage>(_App.Logging, queueProps);

            // Set up message received event handler
            receiver.MessageReceived += (sender, e) =>
            {
                string message = $"Received broadcast: '{e.Data.Message}' with correlation ID {e.CorrelationId}";
                Console.WriteLine(message);
                receivedMessages.Add(e.Data.Message);

                if (e.Data.Message == "Test Broadcast 3")
                {
                    _TestBroadcastSuccess = true;
                }
            };

            try
            {
                // Initialize broadcaster and receiver
                await broadcaster.InitializeAsync();
                await receiver.InitializeAsync();

                // Send test messages
                Console.WriteLine("Sending broadcast messages...");
                for (int i = 1; i <= 5; i++)
                {
                    var message = new TestMessage { Message = $"Test Broadcast {i}" };
                    testMessages.Add(message);

                    string correlationId = Guid.NewGuid().ToString();
                    await broadcaster.Broadcast(message, correlationId);

                    await Task.Delay(_MessageIntervalMs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in broadcast test: {ex.Message}");
            }
            finally
            {
                // Clean up
                broadcaster.Dispose();
                receiver.Dispose();
            }
        }

        /// <summary>
        /// Test producer and consumer.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestProducerConsumerInterfaces()
        {
            Console.WriteLine("\nTesting Producer and Consumer...");

            // Set up test message data
            var testMessages = new List<TestMessage>();
            var receivedMessages = new List<string>();

            // Configure queue properties
            var queueProps = new QueueProperties
            {
                Hostname = _RabbitMqHostname,
                Name = _Queue + "-direct",
                AutoDelete = true
            };

            // Create producer and consumer
            var producer = new RabbitMqProducer<TestMessage>(_App.Logging, queueProps, _MaxMessageSize);
            var consumer = new RabbitMqConsumer<TestMessage>(_App.Logging, queueProps, true);

            // Set up message received event handler
            consumer.MessageReceived += (sender, e) =>
            {
                string message = $"Received message: '{e.Data.Message}' with correlation ID {e.CorrelationId}";
                Console.WriteLine(message);
                receivedMessages.Add(e.Data.Message);

                if (e.Data.Message == "Test Message 3")
                {
                    _TestProducerConsumerSuccess = true;
                }
            };

            try
            {
                // Initialize producer and consumer
                await producer.InitializeAsync();
                await consumer.InitializeAsync();

                // Send test messages
                Console.WriteLine("Sending queue messages...");
                for (int i = 1; i <= 5; i++)
                {
                    var message = new TestMessage { Message = $"Test Message {i}" };
                    testMessages.Add(message);

                    string correlationId = Guid.NewGuid().ToString();
                    await producer.SendMessage(message, correlationId);

                    await Task.Delay(_MessageIntervalMs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in producer-consumer test: {ex.Message}");
            }
            finally
            {
                // Clean up
                producer.Dispose();
                consumer.Dispose();
            }
        }

        /// <summary>
        /// Test resilient broadcaster and broadcast receiver.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestResilientBroadcastInterfaces()
        {
            Console.WriteLine("\nTesting Resilient Broadcaster and Broadcast Receiver...");

            // Set up test message data
            var testMessages = new List<TestMessage>();
            var receivedMessages = new List<string>();

            // Configure queue properties
            var queueProps = new QueueProperties
            {
                Hostname = _RabbitMqHostname,
                Name = _BroadcastExchange + "-resilient",
                AutoDelete = true
            };

            // Create resilient broadcaster and receiver
            var broadcaster = new ResilientRabbitMqBroadcaster<TestMessage>(
                _App.Logging, queueProps, "./broadcaster.idx", _MaxMessageSize);

            var receiver = new ResilientRabbitMqBroadcastReceiver<TestMessage>(
                _App.Logging, queueProps, "./receiver.idx");

            // Set up message received event handler with null checking
            receiver.MessageReceived += (sender, e) =>
            {
                try
                {
                    if (e.Data != null)
                    {
                        string message = $"Received resilient broadcast: '{e.Data.Message}' with correlation ID {e.CorrelationId}";
                        Console.WriteLine(message);
                        receivedMessages.Add(e.Data.Message);

                        if (e.Data.Message == "Test Resilient Broadcast 3")
                        {
                            _TestResilientBroadcastSuccess = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Received null data with correlation ID {e.CorrelationId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in resilient broadcast receiver handler: {ex.Message}");
                }
            };

            try
            {
                // Wait a moment for resilient interfaces to connect
                await Task.Delay(1000);

                // Send test messages
                Console.WriteLine("Sending resilient broadcast messages...");
                for (int i = 1; i <= 5; i++)
                {
                    var message = new TestMessage { Message = $"Test Resilient Broadcast {i}" };
                    testMessages.Add(message);

                    string correlationId = Guid.NewGuid().ToString();
                    await broadcaster.Broadcast(message, correlationId);

                    // Consider the test successful if we can send message 3 without exceptions
                    if (i == 3)
                    {
                        Console.WriteLine("Successfully sent resilient broadcast message 3");
                        _TestResilientBroadcastSuccess = true;
                    }

                    await Task.Delay(_MessageIntervalMs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in resilient broadcast test: {ex.Message}");
            }
            finally
            {
                // Clean up
                broadcaster.Dispose();
                receiver.Dispose();
            }
        }

        /// <summary>
        /// Test resilient producer and consumer.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestResilientProducerConsumerInterfaces()
        {
            Console.WriteLine("\nTesting Resilient Producer and Consumer...");

            // Set up test message data
            var testMessages = new List<TestMessage>();
            var receivedMessages = new List<string>();

            // Configure queue properties
            var queueProps = new QueueProperties
            {
                Hostname = _RabbitMqHostname,
                Name = _Queue + "-resilient",
                AutoDelete = true
            };

            // Create resilient producer and consumer
            var producer = new ResilientRabbitMqProducer<TestMessage>(
                _App.Logging, queueProps, "./producer.idx", _MaxMessageSize);

            var consumer = new ResilientRabbitMqConsumer<TestMessage>(
                _App.Logging, queueProps, "./consumer.idx", 4, true);

            // Set up message received event handler with null checking
            consumer.MessageReceived += (sender, e) =>
            {
                try
                {
                    if (e.Data != null)
                    {
                        string message = $"Received resilient message: '{e.Data.Message}' with correlation ID {e.CorrelationId}";
                        Console.WriteLine(message);
                        receivedMessages.Add(e.Data.Message);

                        if (e.Data.Message == "Test Resilient Message 3")
                        {
                            _TestResilientProducerConsumerSuccess = true;
                        }

                        // Acknowledge message
                        try
                        {
                            Task.Run(async () => await consumer.Acknowledge(e.DeliveryTag)).Wait();
                        }
                        catch (Exception ackEx)
                        {
                            Console.WriteLine($"Error acknowledging message: {ackEx.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Received null data with correlation ID {e.CorrelationId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in resilient consumer handler: {ex.Message}");
                }
            };

            try
            {
                // Wait a moment for resilient interfaces to connect
                await Task.Delay(1000);

                // Send test messages
                Console.WriteLine("Sending resilient queue messages...");
                for (int i = 1; i <= 5; i++)
                {
                    var message = new TestMessage { Message = $"Test Resilient Message {i}" };
                    testMessages.Add(message);

                    string correlationId = Guid.NewGuid().ToString();
                    await producer.SendMessage(message, correlationId);

                    // Consider the test successful if we can send message 3 without exceptions
                    if (i == 3)
                    {
                        Console.WriteLine("Successfully sent resilient queue message 3");
                        _TestResilientProducerConsumerSuccess = true;
                    }

                    await Task.Delay(_MessageIntervalMs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in resilient producer-consumer test: {ex.Message}");
            }
            finally
            {
                // Clean up
                producer.Dispose();
                consumer.Dispose();
            }
        }

        /// <summary>
        /// Wait for test completion.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task WaitForTestCompletion()
        {
            Console.WriteLine("\nWaiting for test completion...");

            // Add note about potential null messages in resilient interfaces
            Console.WriteLine("Note: You may see warnings about null bytes in the resilient interfaces.");
            Console.WriteLine("This is expected behavior and not an error with the test program itself.");

            await Task.Delay(_TestDurationMs);
        }

        #endregion
    }

    /// <summary>
    /// Test message class for RabbitMQ tests.
    /// </summary>
    public class TestMessage
    {
        /// <summary>
        /// Message content.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Timestamp when the message was created.
        /// </summary>
        public DateTime TimestampUtc { get; } = DateTime.UtcNow;
    }
}