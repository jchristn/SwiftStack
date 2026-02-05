namespace Test.Automated
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using SwiftStack;
    using SwiftStack.Rest;
    using SwiftStack.Rest.OpenApi;
    using SwiftStack.Serialization;
    using SwiftStack.Websockets;
    using SwiftStack.RabbitMq;
    using WatsonWebsocket;

    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("SwiftStack Automated Test Suite");
            Console.WriteLine("================================================================================");
            Console.WriteLine();

            var runner = new TestRunner();

            // Register all test suites
            runner.RegisterSuite(new CoreTests());
            runner.RegisterSuite(new SerializationTests());
            runner.RegisterSuite(new ParameterTests());
            runner.RegisterSuite(new RestApiTests());
            runner.RegisterSuite(new OpenApiTests());
            runner.RegisterSuite(new DefaultRouteTests());
            runner.RegisterSuite(new ChunkedTransferTests());
            runner.RegisterSuite(new WebSocketTests());
            runner.RegisterSuite(new RabbitMqTests());

            // Run all tests
            await runner.RunAllAsync();

            // Summary
            runner.PrintSummary();

            // Return exit code (0 = success, 1 = failure)
            return runner.AllPassed ? 0 : 1;
        }
    }

    #region Test Runner Infrastructure

    public class TestRunner
    {
        private List<TestSuite> _suites = new List<TestSuite>();
        private List<TestResult> _allResults = new List<TestResult>();

        public bool AllPassed => _allResults.All(r => r.Passed);

        public void RegisterSuite(TestSuite suite)
        {
            _suites.Add(suite);
        }

        public async Task RunAllAsync()
        {
            foreach (var suite in _suites)
            {
                Console.WriteLine($"--- {suite.Name} ---");
                Console.WriteLine();

                var results = await suite.RunAsync();
                _allResults.AddRange(results);

                Console.WriteLine();
            }
        }

        public void PrintSummary()
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("TEST SUMMARY");
            Console.WriteLine("================================================================================");

            int passed = _allResults.Count(r => r.Passed);
            int failed = _allResults.Count(r => !r.Passed);
            int total = _allResults.Count;

            Console.WriteLine($"Total: {total} | Passed: {passed} | Failed: {failed}");
            Console.WriteLine();

            if (failed > 0)
            {
                Console.WriteLine("Failed Tests:");
                foreach (var result in _allResults.Where(r => !r.Passed))
                {
                    Console.WriteLine($"  - {result.Name}: {result.Message}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("================================================================================");
            if (AllPassed)
            {
                Console.WriteLine("OVERALL RESULT: PASS");
            }
            else
            {
                Console.WriteLine("OVERALL RESULT: FAIL");
            }
            Console.WriteLine("================================================================================");
        }
    }

    public class TestResult
    {
        public string Name { get; set; }
        public bool Passed { get; set; }
        public string Message { get; set; }
    }

    public abstract class TestSuite
    {
        public abstract string Name { get; }
        protected List<TestResult> Results = new List<TestResult>();

        public async Task<List<TestResult>> RunAsync()
        {
            Results.Clear();
            await RunTestsAsync();
            return Results;
        }

        protected abstract Task RunTestsAsync();

        protected void Pass(string testName)
        {
            Results.Add(new TestResult { Name = testName, Passed = true });
            Console.WriteLine($"[PASS] {testName}");
        }

        protected void Fail(string testName, string message)
        {
            Results.Add(new TestResult { Name = testName, Passed = false, Message = message });
            Console.WriteLine($"[FAIL] {testName}");
            Console.WriteLine($"       {message}");
        }

        protected void Skip(string testName, string reason)
        {
            Console.WriteLine($"[SKIP] {testName} - {reason}");
        }
    }

    #endregion

    #region Core Tests

    public class CoreTests : TestSuite
    {
        public override string Name => "Core Functionality Tests";

        protected override async Task RunTestsAsync()
        {
            await Task.Run(() => Test_SwiftStackApp_Creation());
            await Task.Run(() => Test_SwiftStackApp_Name());
            await Task.Run(() => Test_SwiftStackApp_Serializer_Default());
            await Task.Run(() => Test_SwiftStackApp_Serializer_Override());
            await Task.Run(() => Test_SwiftStackException_Creation());
            await Task.Run(() => Test_SwiftStackException_StatusCodes());
            await Task.Run(() => Test_ApiResultEnum_Values());
        }

        private void Test_SwiftStackApp_Creation()
        {
            try
            {
                var app = new SwiftStackApp("Test", quiet: true);
                if (app != null && app.Name == "Test")
                    Pass("SwiftStackApp creation");
                else
                    Fail("SwiftStackApp creation", "App is null or name mismatch");
            }
            catch (Exception ex)
            {
                Fail("SwiftStackApp creation", ex.Message);
            }
        }

        private void Test_SwiftStackApp_Name()
        {
            try
            {
                var app = new SwiftStackApp("MyApp", quiet: true);
                app.Name = "UpdatedName";
                if (app.Name == "UpdatedName")
                    Pass("SwiftStackApp name modification");
                else
                    Fail("SwiftStackApp name modification", $"Expected 'UpdatedName', got '{app.Name}'");
            }
            catch (Exception ex)
            {
                Fail("SwiftStackApp name modification", ex.Message);
            }
        }

        private void Test_SwiftStackApp_Serializer_Default()
        {
            try
            {
                var app = new SwiftStackApp("Test", quiet: true);
                if (app.Serializer != null)
                    Pass("SwiftStackApp default serializer");
                else
                    Fail("SwiftStackApp default serializer", "Serializer is null");
            }
            catch (Exception ex)
            {
                Fail("SwiftStackApp default serializer", ex.Message);
            }
        }

        private void Test_SwiftStackApp_Serializer_Override()
        {
            try
            {
                var app = new SwiftStackApp("Test", quiet: true);
                var customSerializer = new Serializer();
                app.Serializer = customSerializer;

                if (app.Serializer == customSerializer)
                    Pass("SwiftStackApp serializer override");
                else
                    Fail("SwiftStackApp serializer override", "Serializer not properly assigned");
            }
            catch (Exception ex)
            {
                Fail("SwiftStackApp serializer override", ex.Message);
            }
        }

        private void Test_SwiftStackException_Creation()
        {
            try
            {
                var ex = new SwiftStackException(ApiResultEnum.NotFound);
                if (ex.Result == ApiResultEnum.NotFound)
                    Pass("SwiftStackException creation");
                else
                    Fail("SwiftStackException creation", $"Expected NotFound, got {ex.Result}");
            }
            catch (Exception ex)
            {
                Fail("SwiftStackException creation", ex.Message);
            }
        }

        private void Test_SwiftStackException_StatusCodes()
        {
            try
            {
                var tests = new Dictionary<ApiResultEnum, int>
                {
                    { ApiResultEnum.Success, 200 },
                    { ApiResultEnum.Created, 201 },
                    { ApiResultEnum.BadRequest, 400 },
                    { ApiResultEnum.NotAuthorized, 401 },
                    { ApiResultEnum.NotFound, 404 },
                    { ApiResultEnum.Conflict, 409 },
                    { ApiResultEnum.SlowDown, 429 },
                    { ApiResultEnum.InternalError, 500 }
                };

                foreach (var test in tests)
                {
                    var ex = new SwiftStackException(test.Key);
                    if (ex.StatusCode != test.Value)
                    {
                        Fail("SwiftStackException status codes",
                            $"{test.Key} expected {test.Value}, got {ex.StatusCode}");
                        return;
                    }
                }

                Pass("SwiftStackException status codes");
            }
            catch (Exception ex)
            {
                Fail("SwiftStackException status codes", ex.Message);
            }
        }

        private void Test_ApiResultEnum_Values()
        {
            try
            {
                var values = Enum.GetValues<ApiResultEnum>();
                var expectedCount = 9; // Success, NotFound, Created, NotAuthorized, InternalError, SlowDown, Conflict, BadRequest, DeserializationError

                if (values.Length == expectedCount)
                    Pass("ApiResultEnum values count");
                else
                    Fail("ApiResultEnum values count", $"Expected {expectedCount}, got {values.Length}");
            }
            catch (Exception ex)
            {
                Fail("ApiResultEnum values count", ex.Message);
            }
        }
    }

    #endregion

    #region Serialization Tests

    public class SerializationTests : TestSuite
    {
        public override string Name => "Serialization Tests";

        protected override async Task RunTestsAsync()
        {
            await Task.Run(() => Test_Serializer_SerializeObject());
            await Task.Run(() => Test_Serializer_DeserializeObject());
            await Task.Run(() => Test_Serializer_DateTime());
            await Task.Run(() => Test_Serializer_DateTimeFormats());
            await Task.Run(() => Test_Serializer_ComplexObject());
            await Task.Run(() => Test_Serializer_NullValues());
            await Task.Run(() => Test_Serializer_CopyObject());
        }

        private void Test_Serializer_SerializeObject()
        {
            try
            {
                var serializer = new Serializer();
                var obj = new { Name = "Test", Value = 123 };
                string json = serializer.SerializeJson(obj, pretty: false);

                if (!string.IsNullOrEmpty(json) && json.Contains("Test") && json.Contains("123"))
                    Pass("Serializer serialize object");
                else
                    Fail("Serializer serialize object", $"Unexpected JSON: {json}");
            }
            catch (Exception ex)
            {
                Fail("Serializer serialize object", ex.Message);
            }
        }

        private void Test_Serializer_DeserializeObject()
        {
            try
            {
                var serializer = new Serializer();
                string json = "{\"Name\":\"Test\",\"Value\":123}";
                var obj = serializer.DeserializeJson<TestData>(json);

                if (obj != null && obj.Name == "Test" && obj.Value == 123)
                    Pass("Serializer deserialize object");
                else
                    Fail("Serializer deserialize object", "Deserialized object has incorrect values");
            }
            catch (Exception ex)
            {
                Fail("Serializer deserialize object", ex.Message);
            }
        }

        private void Test_Serializer_DateTime()
        {
            try
            {
                var serializer = new Serializer();
                var dt = new DateTime(2025, 11, 16, 14, 30, 45, 123, DateTimeKind.Utc)
                    .AddTicks(4560); // Add microseconds
                var obj = new { Timestamp = dt };
                string json = serializer.SerializeJson(obj, pretty: false);

                // Should be in format: yyyy-MM-ddTHH:mm:ss.ffffffZ
                if (json.Contains("2025-11-16T14:30:45") && json.Contains("Z"))
                    Pass("Serializer DateTime format");
                else
                    Fail("Serializer DateTime format", $"Unexpected format: {json}");
            }
            catch (Exception ex)
            {
                Fail("Serializer DateTime format", ex.Message);
            }
        }

        private void Test_Serializer_DateTimeFormats()
        {
            try
            {
                var serializer = new Serializer();
                var testFormats = new[]
                {
                    "2025-11-16T14:30:45.123456Z",
                    "2025-11-16 14:30:45",
                    "2025-11-16",
                    "11/16/2025 14:30"
                };

                foreach (var format in testFormats)
                {
                    var json = $"{{\"Timestamp\":\"{format}\"}}";
                    var obj = serializer.DeserializeJson<TestTimestamp>(json);
                    if (obj == null || obj.Timestamp == DateTime.MinValue)
                    {
                        Fail("Serializer DateTime parsing", $"Failed to parse: {format}");
                        return;
                    }
                }

                Pass("Serializer DateTime parsing");
            }
            catch (Exception ex)
            {
                Fail("Serializer DateTime parsing", ex.Message);
            }
        }

        private void Test_Serializer_ComplexObject()
        {
            try
            {
                var serializer = new Serializer();
                var obj = new ComplexData
                {
                    Id = 123,
                    Name = "Test",
                    Items = new List<string> { "A", "B", "C" },
                    Metadata = new Dictionary<string, object>
                    {
                        { "Key1", "Value1" },
                        { "Key2", 456 }
                    }
                };

                string json = serializer.SerializeJson(obj);
                var deserialized = serializer.DeserializeJson<ComplexData>(json);

                if (deserialized != null &&
                    deserialized.Id == 123 &&
                    deserialized.Items.Count == 3 &&
                    deserialized.Metadata.Count == 2)
                    Pass("Serializer complex object");
                else
                    Fail("Serializer complex object", "Deserialized object has incorrect structure");
            }
            catch (Exception ex)
            {
                Fail("Serializer complex object", ex.Message);
            }
        }

        private void Test_Serializer_NullValues()
        {
            try
            {
                var serializer = new Serializer();
                var obj = new { Name = "Test", Value = (string)null };
                string json = serializer.SerializeJson(obj, pretty: false);

                // Null values should be omitted by default
                if (!json.Contains("\"Value\""))
                    Pass("Serializer null value handling");
                else
                    Fail("Serializer null value handling", "Null values should be omitted");
            }
            catch (Exception ex)
            {
                Fail("Serializer null value handling", ex.Message);
            }
        }

        private void Test_Serializer_CopyObject()
        {
            try
            {
                var serializer = new Serializer();
                var original = new TestData { Name = "Original", Value = 999 };
                var copy = serializer.CopyObject<TestData>(original);

                if (copy != null && copy.Name == "Original" && copy.Value == 999 && copy != original)
                    Pass("Serializer copy object");
                else
                    Fail("Serializer copy object", "Copy failed or is same reference");
            }
            catch (Exception ex)
            {
                Fail("Serializer copy object", ex.Message);
            }
        }

        private class TestData
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

        private class TestTimestamp
        {
            public DateTime Timestamp { get; set; }
        }

        private class ComplexData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<string> Items { get; set; }
            public Dictionary<string, object> Metadata { get; set; }
        }
    }

    #endregion

    #region Parameter Tests

    public class ParameterTests : TestSuite
    {
        public override string Name => "RequestParameters Tests";

        protected override async Task RunTestsAsync()
        {
            await Task.Run(() => Test_Parameters_String());
            await Task.Run(() => Test_Parameters_Int());
            await Task.Run(() => Test_Parameters_Long());
            await Task.Run(() => Test_Parameters_Double());
            await Task.Run(() => Test_Parameters_Decimal());
            await Task.Run(() => Test_Parameters_Bool());
            await Task.Run(() => Test_Parameters_DateTime());
            await Task.Run(() => Test_Parameters_Guid());
            await Task.Run(() => Test_Parameters_Array());
            await Task.Run(() => Test_Parameters_Contains());
            await Task.Run(() => Test_Parameters_GetKeys());
        }

        private void Test_Parameters_String()
        {
            try
            {
                var nvc = new NameValueCollection { { "key", "value" } };
                var parameters = new RequestParameters(nvc);

                if (parameters["key"] == "value")
                    Pass("RequestParameters string access");
                else
                    Fail("RequestParameters string access", $"Expected 'value', got '{parameters["key"]}'");
            }
            catch (Exception ex)
            {
                Fail("RequestParameters string access", ex.Message);
            }
        }

        private void Test_Parameters_Int()
        {
            try
            {
                var nvc = new NameValueCollection
                {
                    { "int1", "123" },
                    { "int2", "456" }
                };
                var parameters = new RequestParameters(nvc);

                if (parameters.GetInt("int1") == 123 && parameters.GetInt("int2") == 456)
                    Pass("RequestParameters int conversion");
                else
                    Fail("RequestParameters int conversion", "Values don't match");
            }
            catch (Exception ex)
            {
                Fail("RequestParameters int conversion", ex.Message);
            }
        }

        private void Test_Parameters_Long()
        {
            try
            {
                var nvc = new NameValueCollection { { "long", "9223372036854775807" } };
                var parameters = new RequestParameters(nvc);

                if (parameters.GetLong("long") == 9223372036854775807L)
                    Pass("RequestParameters long conversion");
                else
                    Fail("RequestParameters long conversion", "Value doesn't match");
            }
            catch (Exception ex)
            {
                Fail("RequestParameters long conversion", ex.Message);
            }
        }

        private void Test_Parameters_Double()
        {
            try
            {
                var nvc = new NameValueCollection { { "double", "123.456" } };
                var parameters = new RequestParameters(nvc);

                if (Math.Abs(parameters.GetDouble("double") - 123.456) < 0.0001)
                    Pass("RequestParameters double conversion");
                else
                    Fail("RequestParameters double conversion", "Value doesn't match");
            }
            catch (Exception ex)
            {
                Fail("RequestParameters double conversion", ex.Message);
            }
        }

        private void Test_Parameters_Decimal()
        {
            try
            {
                var nvc = new NameValueCollection { { "decimal", "123.45" } };
                var parameters = new RequestParameters(nvc);

                if (parameters.GetDecimal("decimal") == 123.45m)
                    Pass("RequestParameters decimal conversion");
                else
                    Fail("RequestParameters decimal conversion", "Value doesn't match");
            }
            catch (Exception ex)
            {
                Fail("RequestParameters decimal conversion", ex.Message);
            }
        }

        private void Test_Parameters_Bool()
        {
            try
            {
                var nvc = new NameValueCollection
                {
                    { "bool1", "true" },
                    { "bool2", "1" },
                    { "bool3", "yes" },
                    { "bool4", "on" }
                };
                var parameters = new RequestParameters(nvc);

                if (parameters.GetBool("bool1") &&
                    parameters.GetBool("bool2") &&
                    parameters.GetBool("bool3") &&
                    parameters.GetBool("bool4"))
                    Pass("RequestParameters bool conversion");
                else
                    Fail("RequestParameters bool conversion", "Not all values parsed as true");
            }
            catch (Exception ex)
            {
                Fail("RequestParameters bool conversion", ex.Message);
            }
        }

        private void Test_Parameters_DateTime()
        {
            try
            {
                var nvc = new NameValueCollection { { "date", "2025-11-16T14:30:00" } };
                var parameters = new RequestParameters(nvc);
                var dt = parameters.GetDateTime("date");

                if (dt.Year == 2025 && dt.Month == 11 && dt.Day == 16)
                    Pass("RequestParameters DateTime conversion");
                else
                    Fail("RequestParameters DateTime conversion", "Date doesn't match");
            }
            catch (Exception ex)
            {
                Fail("RequestParameters DateTime conversion", ex.Message);
            }
        }

        private void Test_Parameters_Guid()
        {
            try
            {
                var guid = Guid.NewGuid();
                var nvc = new NameValueCollection { { "guid", guid.ToString() } };
                var parameters = new RequestParameters(nvc);
                var parsed = parameters.GetGuid("guid");

                if (parsed == guid)
                    Pass("RequestParameters Guid conversion");
                else
                    Fail("RequestParameters Guid conversion", "GUID doesn't match");
            }
            catch (Exception ex)
            {
                Fail("RequestParameters Guid conversion", ex.Message);
            }
        }

        private void Test_Parameters_Array()
        {
            try
            {
                var nvc = new NameValueCollection { { "array", "a,b,c,d" } };
                var parameters = new RequestParameters(nvc);
                var arr = parameters.GetArray("array");

                if (arr != null && arr.Length == 4 && arr[0] == "a" && arr[3] == "d")
                    Pass("RequestParameters array parsing");
                else
                    Fail("RequestParameters array parsing", "Array doesn't match");
            }
            catch (Exception ex)
            {
                Fail("RequestParameters array parsing", ex.Message);
            }
        }

        private void Test_Parameters_Contains()
        {
            try
            {
                var nvc = new NameValueCollection { { "key", "value" } };
                var parameters = new RequestParameters(nvc);

                if (parameters.Contains("key") && !parameters.Contains("missing"))
                    Pass("RequestParameters Contains");
                else
                    Fail("RequestParameters Contains", "Contains check failed");
            }
            catch (Exception ex)
            {
                Fail("RequestParameters Contains", ex.Message);
            }
        }

        private void Test_Parameters_GetKeys()
        {
            try
            {
                var nvc = new NameValueCollection
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                };
                var parameters = new RequestParameters(nvc);
                var keys = parameters.GetKeys();

                if (keys.Length == 2 && keys.Contains("key1") && keys.Contains("key2"))
                    Pass("RequestParameters GetKeys");
                else
                    Fail("RequestParameters GetKeys", "Keys don't match");
            }
            catch (Exception ex)
            {
                Fail("RequestParameters GetKeys", ex.Message);
            }
        }
    }

    #endregion

    #region REST API Tests

    public class RestApiTests : TestSuite
    {
        public override string Name => "REST API Tests";
        private SwiftStackApp _app;
        private int _testPort = 18888;
        private string _baseUrl;
        private CancellationTokenSource _cts;
        private HttpClient _httpClient;

        protected override async Task RunTestsAsync()
        {
            _baseUrl = $"http://127.0.0.1:{_testPort}";
            Task serverTask = null;

            try
            {
                // Setup
                _app = new SwiftStackApp("TestApp", quiet: true);
                _app.LoggingSettings.EnableConsole = false;
                _app.Rest.WebserverSettings.Hostname = "127.0.0.1";
                _app.Rest.WebserverSettings.Port = _testPort;

                // Register routes
                RegisterTestRoutes();

                // Start server in background
                _cts = new CancellationTokenSource();
                serverTask = Task.Run(async () =>
                {
                    try
                    {
                        await _app.Rest.Run(_cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when canceling
                    }
                });

                // Create reusable HTTP client with optimized settings
                _httpClient = new HttpClient(new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                    MaxConnectionsPerServer = 10
                })
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };

                // Wait for server to start
                await Task.Delay(1000);

                // Run tests with timeout protection
                using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                await Test_Get_Simple();
                await Test_Get_WithQueryParameters();
                await Test_Post_WithBody();
                await Test_Put_WithUrlParameters();
                await Test_Delete_Simple();
                await Test_Response_Null();
                await Test_Response_Tuple();
                await Test_Exception_NotFound();
                await Test_Exception_BadRequest();
                await Test_Authentication_Success();
                await Test_Authentication_Failure();
            }
            catch (Exception ex)
            {
                Fail("REST API setup", ex.Message);
            }
            finally
            {
                // Cleanup - order matters!
                try
                {
                    _httpClient?.Dispose();
                }
                catch { }

                try
                {
                    _cts?.Cancel();
                    await Task.Delay(100); // Let cancellation propagate
                }
                catch { }

                try
                {
                    // Dispose with timeout
                    var disposeTask = Task.Run(() => _app?.Rest?.Dispose());
                    await Task.WhenAny(disposeTask, Task.Delay(500));
                }
                catch { }
            }
        }

        private void RegisterTestRoutes()
        {
            _app.Rest.Get("/", async (req) => "Hello World");

            _app.Rest.Get("/query", async (req) =>
            {
                string name = req.Query["name"];
                int age = req.Query.GetInt("age");
                return new { Name = name, Age = age };
            });

            _app.Rest.Post<PostData>("/post", async (req) =>
            {
                var data = req.GetData<PostData>();
                return new { Received = data.Message };
            });

            _app.Rest.Put<PutData>("/item/{id}", async (req) =>
            {
                string id = req.Parameters["id"];
                var data = req.GetData<PutData>();
                return new { Id = id, Value = data.Value };
            });

            _app.Rest.Delete("/delete", async (req) => null);

            _app.Rest.Get("/tuple", async (req) =>
            {
                return new Tuple<object, int>(new { Data = "Custom" }, 201);
            });

            _app.Rest.Get("/notfound", async (req) =>
            {
                throw new SwiftStackException(ApiResultEnum.NotFound);
            });

            _app.Rest.Get("/badrequest", async (req) =>
            {
                throw new SwiftStackException(ApiResultEnum.BadRequest, "Invalid data");
            });

            _app.Rest.AuthenticationRoute = AuthHandler;

            _app.Rest.Get("/protected", async (req) => "Secret Data", requireAuthentication: true);
        }

        private async Task<AuthResult> AuthHandler(WatsonWebserver.Core.HttpContextBase ctx)
        {
            var auth = ctx.Request.Authorization;
            if (auth != null && auth.BearerToken == "valid-token")
            {
                return new AuthResult
                {
                    AuthenticationResult = AuthenticationResultEnum.Success,
                    AuthorizationResult = AuthorizationResultEnum.Permitted
                };
            }

            return new AuthResult
            {
                AuthenticationResult = AuthenticationResultEnum.Invalid,
                AuthorizationResult = AuthorizationResultEnum.DeniedImplicit
            };
        }

        private async Task Test_Get_Simple()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && content == "Hello World")
                    Pass("REST GET simple");
                else
                    Fail("REST GET simple", $"Status: {response.StatusCode}, Content: {content}");
            }
            catch (Exception ex)
            {
                Fail("REST GET simple", ex.Message);
            }
        }

        private async Task Test_Get_WithQueryParameters()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/query?name=John&age=30");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode &&
                    content.Contains("John") &&
                    content.Contains("30"))
                    Pass("REST GET with query parameters");
                else
                    Fail("REST GET with query parameters", $"Content: {content}");
            }
            catch (Exception ex)
            {
                Fail("REST GET with query parameters", ex.Message);
            }
        }

        private async Task Test_Post_WithBody()
        {
            try
            {
                var data = new PostData { Message = "Test Message" };
                var json = JsonSerializer.Serialize(data);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/post", httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && content.Contains("Test Message"))
                    Pass("REST POST with body");
                else
                    Fail("REST POST with body", $"Content: {content}");
            }
            catch (Exception ex)
            {
                Fail("REST POST with body", ex.Message);
            }
        }

        private async Task Test_Put_WithUrlParameters()
        {
            try
            {
                var data = new PutData { Value = "Updated" };
                var json = JsonSerializer.Serialize(data);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_baseUrl}/item/123", httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode &&
                    content.Contains("123") &&
                    content.Contains("Updated"))
                    Pass("REST PUT with URL parameters");
                else
                    Fail("REST PUT with URL parameters", $"Content: {content}");
            }
            catch (Exception ex)
            {
                Fail("REST PUT with URL parameters", ex.Message);
            }
        }

        private async Task Test_Delete_Simple()
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/delete");

                // Null response may return 200 or 204 depending on implementation
                if (response.IsSuccessStatusCode)
                    Pass("REST DELETE (null response)");
                else
                    Fail("REST DELETE (null response)", $"Expected success, got {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Fail("REST DELETE (null response)", ex.Message);
            }
        }

        private async Task Test_Response_Null()
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/delete");

                // Accept both 200 and 204 for null responses
                if (response.IsSuccessStatusCode)
                    Pass("REST null response status");
                else
                    Fail("REST null response status", $"Expected success, got {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Fail("REST null response status", ex.Message);
            }
        }

        private async Task Test_Response_Tuple()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/tuple");
                var content = await response.Content.ReadAsStringAsync();

                // Check if we got 201 status code (tuple response with custom code)
                if ((int)response.StatusCode == 201)
                    Pass("REST tuple response");
                else
                    Fail("REST tuple response", $"Expected 201, got {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                Fail("REST tuple response", ex.Message);
            }
        }

        private async Task Test_Exception_NotFound()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/notfound");
                var content = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 404)
                    Pass("REST exception NotFound");
                else
                    Fail("REST exception NotFound", $"Expected 404, got {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Fail("REST exception NotFound", ex.Message);
            }
        }

        private async Task Test_Exception_BadRequest()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/badrequest");
                var content = await response.Content.ReadAsStringAsync();

                // Check for 400 status and BadRequest in response
                if ((int)response.StatusCode == 400 && content.Contains("BadRequest"))
                    Pass("REST exception BadRequest");
                else
                    Fail("REST exception BadRequest", $"Status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Fail("REST exception BadRequest", ex.Message);
            }
        }

        private async Task Test_Authentication_Success()
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/protected");
                request.Headers.Add("Authorization", "Bearer valid-token");
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && content.Contains("Secret Data"))
                    Pass("REST authentication success");
                else
                    Fail("REST authentication success", $"Status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Fail("REST authentication success", ex.Message);
            }
        }

        private async Task Test_Authentication_Failure()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/protected");

                if ((int)response.StatusCode == 401)
                    Pass("REST authentication failure");
                else
                    Fail("REST authentication failure", $"Expected 401, got {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Fail("REST authentication failure", ex.Message);
            }
        }

        private class PostData
        {
            public string Message { get; set; }
        }

        private class PutData
        {
            public string Value { get; set; }
        }
    }

    #endregion

    #region OpenAPI Tests

    public class OpenApiTests : TestSuite
    {
        public override string Name => "OpenAPI Tests";
        private SwiftStackApp _app;
        private int _testPort = 18889;
        private string _baseUrl;
        private CancellationTokenSource _cts;
        private HttpClient _httpClient;

        protected override async Task RunTestsAsync()
        {
            _baseUrl = $"http://127.0.0.1:{_testPort}";
            Task serverTask = null;

            try
            {
                // Setup
                _app = new SwiftStackApp("TestApp", quiet: true);
                _app.LoggingSettings.EnableConsole = false;
                _app.Rest.WebserverSettings.Hostname = "127.0.0.1";
                _app.Rest.WebserverSettings.Port = _testPort;

                // Enable OpenAPI
                _app.Rest.UseOpenApi(openApi =>
                {
                    openApi.Info.Title = "Test API";
                    openApi.Info.Version = "1.0.0";
                    openApi.Info.Description = "Test API for OpenAPI validation";

                    openApi.Tags.Add(new OpenApiTag("Users", "User operations"));
                    openApi.SecuritySchemes["Bearer"] = OpenApiSecurityScheme.Bearer("JWT");
                });

                // Register test routes with OpenAPI metadata
                RegisterTestRoutes();

                // Start server in background
                _cts = new CancellationTokenSource();
                serverTask = Task.Run(async () =>
                {
                    try
                    {
                        await _app.Rest.Run(_cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when canceling
                    }
                });

                // Create HTTP client
                _httpClient = new HttpClient(new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                    MaxConnectionsPerServer = 10
                })
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };

                // Wait for server to start
                await Task.Delay(1000);

                // Run unit tests (no server required)
                await Task.Run(() => Test_Schema_PrimitiveTypes());
                await Task.Run(() => Test_Schema_ComplexObject());
                await Task.Run(() => Test_Schema_Enum());
                await Task.Run(() => Test_Schema_Array());
                await Task.Run(() => Test_RouteMetadata_FluentApi());

                // Run integration tests
                await Test_OpenApiEndpoint_ReturnsJson();
                await Test_OpenApiEndpoint_ContainsInfo();
                await Test_OpenApiEndpoint_ContainsPaths();
                await Test_SwaggerUi_ReturnsHtml();
            }
            catch (Exception ex)
            {
                Fail("OpenAPI setup", ex.Message);
            }
            finally
            {
                try
                {
                    _httpClient?.Dispose();
                }
                catch { }

                try
                {
                    _cts?.Cancel();
                    await Task.Delay(100);
                }
                catch { }

                try
                {
                    var disposeTask = Task.Run(() => _app?.Rest?.Dispose());
                    await Task.WhenAny(disposeTask, Task.Delay(500));
                }
                catch { }
            }
        }

        private void RegisterTestRoutes()
        {
            _app.Rest.Get("/users", async (req) => new[] { new TestUser { Id = 1, Name = "Test" } },
                api => api
                    .WithTag("Users")
                    .WithSummary("Get all users")
                    .WithResponse(200, OpenApiResponseMetadata.Json("List of users", OpenApiSchemaMetadata.Array(OpenApiSchemaMetadata.FromType<TestUser>()))));

            _app.Rest.Get("/users/{id}", async (req) => new TestUser { Id = 1, Name = "Test" },
                api => api
                    .WithTag("Users")
                    .WithSummary("Get user by ID")
                    .WithParameter(OpenApiParameterMetadata.Path("id", "User ID")));

            _app.Rest.Post<TestUser>("/users", async (req) => req.GetData<TestUser>(),
                api => api
                    .WithTag("Users")
                    .WithSummary("Create user")
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json<TestUser>("User to create")));

            _app.Rest.AuthenticationRoute = async (ctx) => new AuthResult
            {
                AuthenticationResult = AuthenticationResultEnum.Success,
                AuthorizationResult = AuthorizationResultEnum.Permitted
            };

            _app.Rest.Get("/protected", async (req) => "Secret",
                api => api
                    .WithSummary("Protected endpoint")
                    .WithSecurity("Bearer"),
                requireAuthentication: true);
        }

        private void Test_Schema_PrimitiveTypes()
        {
            try
            {
                OpenApiSchemaMetadata intSchema = OpenApiSchemaMetadata.FromType<int>();
                OpenApiSchemaMetadata strSchema = OpenApiSchemaMetadata.FromType<string>();
                OpenApiSchemaMetadata boolSchema = OpenApiSchemaMetadata.FromType<bool>();

                if (intSchema.Type == "integer" && strSchema.Type == "string" && boolSchema.Type == "boolean")
                    Pass("OpenAPI schema primitive types");
                else
                    Fail("OpenAPI schema primitive types", $"int={intSchema.Type}, string={strSchema.Type}, bool={boolSchema.Type}");
            }
            catch (Exception ex)
            {
                Fail("OpenAPI schema primitive types", ex.Message);
            }
        }

        private void Test_Schema_ComplexObject()
        {
            try
            {
                OpenApiSchemaMetadata schema = OpenApiSchemaMetadata.FromType<TestUser>();

                if (schema.Type == "object" &&
                    schema.Properties != null &&
                    schema.Properties.ContainsKey("Id") &&
                    schema.Properties.ContainsKey("Name"))
                    Pass("OpenAPI schema complex object");
                else
                    Fail("OpenAPI schema complex object", "Object schema missing expected properties");
            }
            catch (Exception ex)
            {
                Fail("OpenAPI schema complex object", ex.Message);
            }
        }

        private void Test_Schema_Enum()
        {
            try
            {
                OpenApiSchemaMetadata schema = OpenApiSchemaMetadata.FromType<TestStatus>();

                if (schema.Type == "string" &&
                    schema.Enum != null &&
                    schema.Enum.Count == 3)
                    Pass("OpenAPI schema enum");
                else
                    Fail("OpenAPI schema enum", $"Type={schema.Type}, Enum count={schema.Enum?.Count}");
            }
            catch (Exception ex)
            {
                Fail("OpenAPI schema enum", ex.Message);
            }
        }

        private void Test_Schema_Array()
        {
            try
            {
                OpenApiSchemaMetadata schema = OpenApiSchemaMetadata.FromType<List<string>>();

                if (schema.Type == "array" && schema.Items != null && schema.Items.Type == "string")
                    Pass("OpenAPI schema array");
                else
                    Fail("OpenAPI schema array", $"Type={schema.Type}, Items type={schema.Items?.Type}");
            }
            catch (Exception ex)
            {
                Fail("OpenAPI schema array", ex.Message);
            }
        }

        private void Test_RouteMetadata_FluentApi()
        {
            try
            {
                OpenApiRouteMetadata metadata = new OpenApiRouteMetadata()
                    .WithTag("TestTag")
                    .WithSummary("Test summary")
                    .WithDescription("Test description")
                    .WithParameter(OpenApiParameterMetadata.Path("id"))
                    .WithResponse(200, OpenApiResponseMetadata.Ok(OpenApiSchemaMetadata.String()))
                    .WithSecurity("Bearer");

                if (metadata.Tags != null && metadata.Tags.Contains("TestTag") &&
                    metadata.Summary == "Test summary" &&
                    metadata.Parameters != null && metadata.Parameters.Count == 1 &&
                    metadata.Responses != null && metadata.Responses.ContainsKey("200") &&
                    metadata.Security != null && metadata.Security.Count == 1)
                    Pass("OpenAPI route metadata fluent API");
                else
                    Fail("OpenAPI route metadata fluent API", "Metadata not properly configured");
            }
            catch (Exception ex)
            {
                Fail("OpenAPI route metadata fluent API", ex.Message);
            }
        }

        private async Task Test_OpenApiEndpoint_ReturnsJson()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/openapi.json");
                string content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode &&
                    response.Content.Headers.ContentType?.MediaType == "application/json")
                    Pass("OpenAPI endpoint returns JSON");
                else
                    Fail("OpenAPI endpoint returns JSON", $"Status: {response.StatusCode}, ContentType: {response.Content.Headers.ContentType?.MediaType}");
            }
            catch (Exception ex)
            {
                Fail("OpenAPI endpoint returns JSON", ex.Message);
            }
        }

        private async Task Test_OpenApiEndpoint_ContainsInfo()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/openapi.json");
                string content = await response.Content.ReadAsStringAsync();

                if (content.Contains("\"openapi\"") &&
                    content.Contains("\"info\"") &&
                    content.Contains("Test API"))
                    Pass("OpenAPI endpoint contains info");
                else
                    Fail("OpenAPI endpoint contains info", "Missing required OpenAPI fields");
            }
            catch (Exception ex)
            {
                Fail("OpenAPI endpoint contains info", ex.Message);
            }
        }

        private async Task Test_OpenApiEndpoint_ContainsPaths()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/openapi.json");
                string content = await response.Content.ReadAsStringAsync();

                if (content.Contains("\"paths\"") &&
                    content.Contains("/users") &&
                    content.Contains("/users/{id}"))
                    Pass("OpenAPI endpoint contains paths");
                else
                    Fail("OpenAPI endpoint contains paths", "Missing paths in OpenAPI document");
            }
            catch (Exception ex)
            {
                Fail("OpenAPI endpoint contains paths", ex.Message);
            }
        }

        private async Task Test_SwaggerUi_ReturnsHtml()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/swagger");
                string content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode &&
                    content.Contains("<!DOCTYPE html>") &&
                    content.Contains("swagger-ui") &&
                    content.Contains("/openapi.json"))
                    Pass("Swagger UI returns HTML");
                else
                    Fail("Swagger UI returns HTML", "Invalid Swagger UI response");
            }
            catch (Exception ex)
            {
                Fail("Swagger UI returns HTML", ex.Message);
            }
        }

        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
        }

        private enum TestStatus
        {
            Active,
            Inactive,
            Pending
        }
    }

    #endregion

    #region Default Route Tests

    public class DefaultRouteTests : TestSuite
    {
        public override string Name => "Default Route Tests";
        private SwiftStackApp _app;
        private int _testPort = 18890;
        private string _baseUrl;
        private CancellationTokenSource _cts;
        private HttpClient _httpClient;

        protected override async Task RunTestsAsync()
        {
            _baseUrl = $"http://127.0.0.1:{_testPort}";

            try
            {
                // Setup
                _app = new SwiftStackApp("TestApp", quiet: true);
                _app.LoggingSettings.EnableConsole = false;
                _app.Rest.WebserverSettings.Hostname = "127.0.0.1";
                _app.Rest.WebserverSettings.Port = _testPort;

                // Register a specific route
                _app.Rest.Get("/api/hello", async (req) => new { Message = "Hello" });

                // Set a custom default route
                _app.Rest.DefaultRoute = async (WatsonWebserver.Core.HttpContextBase ctx) =>
                {
                    string path = ctx.Request.Url.RawWithoutQuery;

                    // Return 200 for specific paths
                    if (path == "/custom-ok")
                    {
                        ctx.Response.StatusCode = 200;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.Send("{\"status\": \"ok\", \"route\": \"custom\"}");
                        return;
                    }

                    // Return 404 for unknown paths
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send("{\"error\": \"Not Found\", \"path\": \"" + path + "\"}");
                };

                // Start server in background
                _cts = new CancellationTokenSource();
                Task serverTask = Task.Run(async () =>
                {
                    try
                    {
                        await _app.Rest.Run(_cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when canceling
                    }
                });

                // Create HTTP client
                _httpClient = new HttpClient(new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                    MaxConnectionsPerServer = 10
                })
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };

                // Wait for server to start
                await Task.Delay(1000);

                // Run tests
                await Test_DefaultRoute_NotSet_Returns400();
                await Test_DefaultRoute_Custom_Returns200();
                await Test_DefaultRoute_Custom_Returns404();
                await Test_DefaultRoute_SpecificRoute_StillWorks();
            }
            catch (Exception ex)
            {
                Fail("Default Route setup", ex.Message);
            }
            finally
            {
                try
                {
                    _httpClient?.Dispose();
                }
                catch { }

                try
                {
                    _cts?.Cancel();
                    await Task.Delay(100);
                }
                catch { }

                try
                {
                    Task disposeTask = Task.Run(() => _app?.Rest?.Dispose());
                    await Task.WhenAny(disposeTask, Task.Delay(500));
                }
                catch { }
            }
        }

        private async Task Test_DefaultRoute_NotSet_Returns400()
        {
            // This test validates that without a custom DefaultRoute,
            // the built-in handler would return 400.
            // Since we set a custom DefaultRoute, we test that separately.
            // Here we verify that a path that doesn't match any route
            // goes through our custom default route (which returns 404).
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/nonexistent-path");
                string content = await response.Content.ReadAsStringAsync();

                // Our custom route returns 404 for unknown paths
                if ((int)response.StatusCode == 404 && content.Contains("/nonexistent-path"))
                    Pass("Default route handles unmatched paths");
                else
                    Fail("Default route handles unmatched paths", $"Status: {response.StatusCode}, Content: {content}");
            }
            catch (Exception ex)
            {
                Fail("Default route handles unmatched paths", ex.Message);
            }
        }

        private async Task Test_DefaultRoute_Custom_Returns200()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/custom-ok");
                string content = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 200 &&
                    content.Contains("\"status\": \"ok\"") &&
                    content.Contains("\"route\": \"custom\""))
                    Pass("Default route returns 200");
                else
                    Fail("Default route returns 200", $"Status: {response.StatusCode}, Content: {content}");
            }
            catch (Exception ex)
            {
                Fail("Default route returns 200", ex.Message);
            }
        }

        private async Task Test_DefaultRoute_Custom_Returns404()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/unknown-path");
                string content = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 404 &&
                    content.Contains("\"error\": \"Not Found\"") &&
                    content.Contains("/unknown-path"))
                    Pass("Default route returns 404");
                else
                    Fail("Default route returns 404", $"Status: {response.StatusCode}, Content: {content}");
            }
            catch (Exception ex)
            {
                Fail("Default route returns 404", ex.Message);
            }
        }

        private async Task Test_DefaultRoute_SpecificRoute_StillWorks()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/api/hello");
                string content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && content.Contains("Hello"))
                    Pass("Specific routes still work with default route set");
                else
                    Fail("Specific routes still work with default route set", $"Status: {response.StatusCode}, Content: {content}");
            }
            catch (Exception ex)
            {
                Fail("Specific routes still work with default route set", ex.Message);
            }
        }
    }

    #endregion

    #region Chunked Transfer Tests

    public class ChunkedTransferTests : TestSuite
    {
        public override string Name => "Chunked Transfer Encoding Tests";
        private SwiftStackApp _App;
        private int _TestPort = 18891;
        private string _BaseUrl;
        private CancellationTokenSource _Cts;
        private HttpClient _HttpClient;

        protected override async Task RunTestsAsync()
        {
            _BaseUrl = $"http://127.0.0.1:{_TestPort}";

            try
            {
                // Setup
                _App = new SwiftStackApp("TestApp", quiet: true);
                _App.LoggingSettings.EnableConsole = false;
                _App.Rest.WebserverSettings.Hostname = "127.0.0.1";
                _App.Rest.WebserverSettings.Port = _TestPort;

                // Register routes
                RegisterChunkedRoutes();

                // Start server in background
                _Cts = new CancellationTokenSource();
                Task serverTask = Task.Run(async () =>
                {
                    try
                    {
                        await _App.Rest.Run(_Cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when canceling
                    }
                });

                // Create reusable HTTP client with optimized settings
                _HttpClient = new HttpClient(new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                    MaxConnectionsPerServer = 10
                })
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                // Wait for server to start
                await Task.Delay(1000);

                // Run tests
                await Test_Chunked_BasicResponse();
                await Test_Chunked_MultipleChunks();
                await Test_Chunked_EmptyResponse();
                await Test_Chunked_LargePayload();
                await Test_Chunked_BinaryData();
                await Test_Chunked_SingleByteChunks();
                await Test_Chunked_WithJsonContentType();
                await Test_Chunked_CustomStatusCode();
                await Test_Chunked_RequestEcho();
                await Test_Chunked_RequestDetection();
            }
            catch (Exception ex)
            {
                Fail("Chunked Transfer setup", ex.Message);
            }
            finally
            {
                try
                {
                    _HttpClient?.Dispose();
                }
                catch { }

                try
                {
                    _Cts?.Cancel();
                    await Task.Delay(100);
                }
                catch { }

                try
                {
                    Task disposeTask = Task.Run(() => _App?.Rest?.Dispose());
                    await Task.WhenAny(disposeTask, Task.Delay(500));
                }
                catch { }
            }
        }

        private void RegisterChunkedRoutes()
        {
            // GET /chunked/basic - Send "Hello, chunked world!" as one chunk + empty final
            _App.Rest.Get("/chunked/basic", async (req) =>
            {
                req.Http.Response.ChunkedTransfer = true;

                byte[] data = Encoding.UTF8.GetBytes("Hello, chunked world!");
                await req.Http.Response.SendChunk(data, false, CancellationToken.None).ConfigureAwait(false);
                await req.Http.Response.SendChunk(Array.Empty<byte>(), true, CancellationToken.None).ConfigureAwait(false);

                return null;
            });

            // GET /chunked/multi - Send 5 numbered text chunks
            _App.Rest.Get("/chunked/multi", async (req) =>
            {
                req.Http.Response.ChunkedTransfer = true;

                for (int i = 0; i < 5; i++)
                {
                    byte[] chunk = Encoding.UTF8.GetBytes("Chunk " + i + "\n");
                    await req.Http.Response.SendChunk(chunk, false, CancellationToken.None).ConfigureAwait(false);
                }

                await req.Http.Response.SendChunk(Array.Empty<byte>(), true, CancellationToken.None).ConfigureAwait(false);

                return null;
            });

            // GET /chunked/empty - Set chunked mode, send only a final empty chunk
            _App.Rest.Get("/chunked/empty", async (req) =>
            {
                req.Http.Response.ChunkedTransfer = true;

                await req.Http.Response.SendChunk(Array.Empty<byte>(), true, CancellationToken.None).ConfigureAwait(false);

                return null;
            });

            // GET /chunked/large - Send ~100KB payload in 10KB chunks
            _App.Rest.Get("/chunked/large", async (req) =>
            {
                req.Http.Response.ChunkedTransfer = true;

                int chunkSize = 10240; // 10KB
                int totalChunks = 10;  // 10 chunks = ~100KB

                for (int i = 0; i < totalChunks; i++)
                {
                    byte[] chunk = new byte[chunkSize];
                    for (int j = 0; j < chunkSize; j++)
                    {
                        chunk[j] = (byte)((i * chunkSize + j) % 256);
                    }
                    await req.Http.Response.SendChunk(chunk, false, CancellationToken.None).ConfigureAwait(false);
                }

                await req.Http.Response.SendChunk(Array.Empty<byte>(), true, CancellationToken.None).ConfigureAwait(false);

                return null;
            });

            // GET /chunked/binary - Send a 256-byte pattern (0x00-0xFF) in 64-byte chunks
            _App.Rest.Get("/chunked/binary", async (req) =>
            {
                req.Http.Response.ChunkedTransfer = true;

                byte[] fullPattern = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    fullPattern[i] = (byte)i;
                }

                for (int offset = 0; offset < 256; offset += 64)
                {
                    byte[] chunk = new byte[64];
                    Array.Copy(fullPattern, offset, chunk, 0, 64);
                    await req.Http.Response.SendChunk(chunk, false, CancellationToken.None).ConfigureAwait(false);
                }

                await req.Http.Response.SendChunk(Array.Empty<byte>(), true, CancellationToken.None).ConfigureAwait(false);

                return null;
            });

            // GET /chunked/single-byte - Send "ABCDEFGHIJ" one byte at a time
            _App.Rest.Get("/chunked/single-byte", async (req) =>
            {
                req.Http.Response.ChunkedTransfer = true;

                string text = "ABCDEFGHIJ";
                foreach (char c in text)
                {
                    byte[] chunk = new byte[] { (byte)c };
                    await req.Http.Response.SendChunk(chunk, false, CancellationToken.None).ConfigureAwait(false);
                }

                await req.Http.Response.SendChunk(Array.Empty<byte>(), true, CancellationToken.None).ConfigureAwait(false);

                return null;
            });

            // GET /chunked/json - Set Content-Type to application/json, send JSON in chunks
            _App.Rest.Get("/chunked/json", async (req) =>
            {
                req.Http.Response.ChunkedTransfer = true;
                req.Http.Response.ContentType = "application/json";

                string jsonPart1 = "{\"name\":\"chunked\"";
                string jsonPart2 = ",\"value\":42}";

                await req.Http.Response.SendChunk(
                    Encoding.UTF8.GetBytes(jsonPart1), false, CancellationToken.None).ConfigureAwait(false);
                await req.Http.Response.SendChunk(
                    Encoding.UTF8.GetBytes(jsonPart2), false, CancellationToken.None).ConfigureAwait(false);
                await req.Http.Response.SendChunk(
                    Array.Empty<byte>(), true, CancellationToken.None).ConfigureAwait(false);

                return null;
            });

            // GET /chunked/status/{code} - Set custom status code, send chunked response
            _App.Rest.Get("/chunked/status/{code}", async (req) =>
            {
                int statusCode = Convert.ToInt32(req.Parameters["code"]);
                req.Http.Response.StatusCode = statusCode;
                req.Http.Response.ChunkedTransfer = true;

                byte[] data = Encoding.UTF8.GetBytes("Custom status response");
                await req.Http.Response.SendChunk(data, false, CancellationToken.None).ConfigureAwait(false);
                await req.Http.Response.SendChunk(Array.Empty<byte>(), true, CancellationToken.None).ConfigureAwait(false);

                return null;
            });

            // POST /chunked/echo - Read buffered request body, echo it back as chunked response
            _App.Rest.Post<string>("/chunked/echo", async (req) =>
            {
                string body = req.Http.Request.DataAsString;
                req.Http.Response.ChunkedTransfer = true;

                byte[] data = Encoding.UTF8.GetBytes(body);
                await req.Http.Response.SendChunk(data, false, CancellationToken.None).ConfigureAwait(false);
                await req.Http.Response.SendChunk(Array.Empty<byte>(), true, CancellationToken.None).ConfigureAwait(false);

                return null;
            });

            // POST /chunked/receive - Verify ChunkedTransfer on request, respond with confirmation
            _App.Rest.Post<string>("/chunked/receive", async (req) =>
            {
                bool isChunked = req.Http.Request.ChunkedTransfer;
                string body = req.Http.Request.DataAsString;

                req.Http.Response.ChunkedTransfer = true;
                req.Http.Response.ContentType = "application/json";

                string json = "{\"chunked\":" + (isChunked ? "true" : "false") +
                              ",\"bodyLength\":" + (body != null ? body.Length : 0) + "}";

                byte[] data = Encoding.UTF8.GetBytes(json);
                await req.Http.Response.SendChunk(data, false, CancellationToken.None).ConfigureAwait(false);
                await req.Http.Response.SendChunk(Array.Empty<byte>(), true, CancellationToken.None).ConfigureAwait(false);

                return null;
            });
        }

        private async Task Test_Chunked_BasicResponse()
        {
            try
            {
                HttpResponseMessage response = await _HttpClient.GetAsync($"{_BaseUrl}/chunked/basic");
                string content = await response.Content.ReadAsStringAsync();

                bool hasChunkedHeader = response.Headers.TransferEncodingChunked == true;

                if (response.IsSuccessStatusCode && content == "Hello, chunked world!" && hasChunkedHeader)
                    Pass("Chunked basic response");
                else
                    Fail("Chunked basic response",
                        $"Status: {response.StatusCode}, Content: '{content}', Chunked header: {hasChunkedHeader}");
            }
            catch (Exception ex)
            {
                Fail("Chunked basic response", ex.Message);
            }
        }

        private async Task Test_Chunked_MultipleChunks()
        {
            try
            {
                HttpResponseMessage response = await _HttpClient.GetAsync($"{_BaseUrl}/chunked/multi");
                string content = await response.Content.ReadAsStringAsync();

                string expected = "Chunk 0\nChunk 1\nChunk 2\nChunk 3\nChunk 4\n";

                if (response.IsSuccessStatusCode && content == expected)
                    Pass("Chunked multiple chunks");
                else
                    Fail("Chunked multiple chunks",
                        $"Status: {response.StatusCode}, Expected length: {expected.Length}, Got length: {content.Length}");
            }
            catch (Exception ex)
            {
                Fail("Chunked multiple chunks", ex.Message);
            }
        }

        private async Task Test_Chunked_EmptyResponse()
        {
            try
            {
                HttpResponseMessage response = await _HttpClient.GetAsync($"{_BaseUrl}/chunked/empty");
                string content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && content.Length == 0)
                    Pass("Chunked empty response");
                else
                    Fail("Chunked empty response",
                        $"Status: {response.StatusCode}, Content length: {content.Length}");
            }
            catch (Exception ex)
            {
                Fail("Chunked empty response", ex.Message);
            }
        }

        private async Task Test_Chunked_LargePayload()
        {
            try
            {
                HttpResponseMessage response = await _HttpClient.GetAsync($"{_BaseUrl}/chunked/large");
                byte[] content = await response.Content.ReadAsByteArrayAsync();

                int expectedSize = 10240 * 10; // 100KB
                bool sizeCorrect = content.Length == expectedSize;

                // Verify content integrity - check a sample of bytes
                bool contentCorrect = true;
                for (int i = 0; i < content.Length && contentCorrect; i += 1024)
                {
                    if (content[i] != (byte)(i % 256))
                    {
                        contentCorrect = false;
                    }
                }

                if (response.IsSuccessStatusCode && sizeCorrect && contentCorrect)
                    Pass("Chunked large payload");
                else
                    Fail("Chunked large payload",
                        $"Status: {response.StatusCode}, Expected: {expectedSize} bytes, Got: {content.Length} bytes, Integrity: {contentCorrect}");
            }
            catch (Exception ex)
            {
                Fail("Chunked large payload", ex.Message);
            }
        }

        private async Task Test_Chunked_BinaryData()
        {
            try
            {
                HttpResponseMessage response = await _HttpClient.GetAsync($"{_BaseUrl}/chunked/binary");
                byte[] content = await response.Content.ReadAsByteArrayAsync();

                bool sizeCorrect = content.Length == 256;
                bool patternCorrect = true;

                for (int i = 0; i < content.Length && patternCorrect; i++)
                {
                    if (content[i] != (byte)i)
                    {
                        patternCorrect = false;
                    }
                }

                if (response.IsSuccessStatusCode && sizeCorrect && patternCorrect)
                    Pass("Chunked binary data");
                else
                    Fail("Chunked binary data",
                        $"Status: {response.StatusCode}, Size correct: {sizeCorrect}, Pattern correct: {patternCorrect}");
            }
            catch (Exception ex)
            {
                Fail("Chunked binary data", ex.Message);
            }
        }

        private async Task Test_Chunked_SingleByteChunks()
        {
            try
            {
                HttpResponseMessage response = await _HttpClient.GetAsync($"{_BaseUrl}/chunked/single-byte");
                string content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && content == "ABCDEFGHIJ")
                    Pass("Chunked single byte chunks");
                else
                    Fail("Chunked single byte chunks",
                        $"Status: {response.StatusCode}, Content: '{content}'");
            }
            catch (Exception ex)
            {
                Fail("Chunked single byte chunks", ex.Message);
            }
        }

        private async Task Test_Chunked_WithJsonContentType()
        {
            try
            {
                HttpResponseMessage response = await _HttpClient.GetAsync($"{_BaseUrl}/chunked/json");
                string content = await response.Content.ReadAsStringAsync();
                string contentType = response.Content.Headers.ContentType?.MediaType;

                // Verify it's valid JSON by parsing
                bool validJson = false;
                try
                {
                    JsonDocument doc = JsonDocument.Parse(content);
                    validJson = doc.RootElement.GetProperty("name").GetString() == "chunked"
                             && doc.RootElement.GetProperty("value").GetInt32() == 42;
                }
                catch
                {
                    validJson = false;
                }

                if (response.IsSuccessStatusCode && contentType == "application/json" && validJson)
                    Pass("Chunked with JSON content type");
                else
                    Fail("Chunked with JSON content type",
                        $"Status: {response.StatusCode}, ContentType: {contentType}, ValidJson: {validJson}, Content: '{content}'");
            }
            catch (Exception ex)
            {
                Fail("Chunked with JSON content type", ex.Message);
            }
        }

        private async Task Test_Chunked_CustomStatusCode()
        {
            try
            {
                HttpResponseMessage response = await _HttpClient.GetAsync($"{_BaseUrl}/chunked/status/201");
                string content = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 201 && content == "Custom status response")
                    Pass("Chunked custom status code");
                else
                    Fail("Chunked custom status code",
                        $"Expected 201, got {(int)response.StatusCode}, Content: '{content}'");
            }
            catch (Exception ex)
            {
                Fail("Chunked custom status code", ex.Message);
            }
        }

        private async Task Test_Chunked_RequestEcho()
        {
            try
            {
                string testBody = "This is test data for chunked echo";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{_BaseUrl}/chunked/echo");
                request.Content = new StringContent(testBody, Encoding.UTF8, "text/plain");
                request.Headers.TransferEncodingChunked = true;

                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && content == testBody)
                    Pass("Chunked request echo");
                else
                    Fail("Chunked request echo",
                        $"Status: {response.StatusCode}, Expected: '{testBody}', Got: '{content}'");
            }
            catch (Exception ex)
            {
                Fail("Chunked request echo", ex.Message);
            }
        }

        private async Task Test_Chunked_RequestDetection()
        {
            try
            {
                string testBody = "Chunked detection test";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{_BaseUrl}/chunked/receive");
                request.Content = new StringContent(testBody, Encoding.UTF8, "text/plain");
                request.Headers.TransferEncodingChunked = true;

                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();

                bool validResponse = false;
                try
                {
                    JsonDocument doc = JsonDocument.Parse(content);
                    bool isChunked = doc.RootElement.GetProperty("chunked").GetBoolean();
                    int bodyLength = doc.RootElement.GetProperty("bodyLength").GetInt32();
                    validResponse = isChunked && bodyLength > 0;
                }
                catch
                {
                    validResponse = false;
                }

                if (response.IsSuccessStatusCode && validResponse)
                    Pass("Chunked request detection");
                else
                    Fail("Chunked request detection",
                        $"Status: {response.StatusCode}, Content: '{content}'");
            }
            catch (Exception ex)
            {
                Fail("Chunked request detection", ex.Message);
            }
        }
    }

    #endregion

    #region WebSocket Tests

    public class WebSocketTests : TestSuite
    {
        public override string Name => "WebSocket Tests";
        private SwiftStackApp _app;
        private WebsocketsApp _wsApp;
        private int _testPort = 19006;
        private CancellationTokenSource _cts;
        private bool _connectionReceived = false;
        private bool _disconnectionReceived = false;
        private bool _messageReceived = false;
        private string _receivedMessage = null;

        protected override async Task RunTestsAsync()
        {
            Task serverTask = null;

            try
            {
                // Setup
                _app = new SwiftStackApp("TestApp", quiet: true);
                _app.LoggingSettings.EnableConsole = false;

                _wsApp = new WebsocketsApp(_app);
                _wsApp.WebsocketSettings = new WatsonWebsocket.WebsocketSettings
                {
                    Hostnames = new List<string> { "127.0.0.1" },
                    Port = _testPort,
                    Ssl = false
                };

                // Setup event handlers
                _wsApp.OnConnection += (sender, e) => _connectionReceived = true;
                _wsApp.OnDisconnection += (sender, e) => _disconnectionReceived = true;

                // Setup default route for non-routed messages
                _wsApp.DefaultRoute += async (sender, msg) =>
                {
                    await msg.RespondAsync(msg.DataAsString());
                };

                _wsApp.AddRoute("echo", async (msg, token) =>
                {
                    _messageReceived = true;
                    _receivedMessage = msg.DataAsString();
                    await msg.RespondAsync("Echo: " + msg.DataAsString());
                });

                // Start server in background
                _cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                serverTask = Task.Run(async () =>
                {
                    try
                    {
                        await _wsApp.Run(_cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when canceling
                    }
                });

                // Wait for server to start
                await Task.Delay(500);

                // Run tests
                await Test_WebSocket_Connection();
                await Test_WebSocket_SendReceive();
                await Test_WebSocket_Route();
                await Test_WebSocket_Disconnection();
            }
            catch (Exception ex)
            {
                Fail("WebSocket setup", ex.Message);
            }
            finally
            {
                try
                {
                    _cts?.Cancel();
                }
                catch { }

                try
                {
                    _wsApp?.Dispose();
                }
                catch { }

                // Give server time to shutdown
                await Task.Delay(500);
            }
        }

        private async Task Test_WebSocket_Connection()
        {
            try
            {
                _connectionReceived = false;

                var client = new WatsonWsClient("127.0.0.1", _testPort, false);
                await client.StartAsync();

                await Task.Delay(500);

                if (_connectionReceived)
                    Pass("WebSocket connection");
                else
                    Fail("WebSocket connection", "Connection event not received");

                client.Stop();
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Fail("WebSocket connection", ex.Message);
            }
        }

        private async Task Test_WebSocket_SendReceive()
        {
            try
            {
                bool received = false;
                string responseMsg = null;

                var client = new WatsonWsClient("127.0.0.1", _testPort, false);
                client.MessageReceived += (sender, e) =>
                {
                    received = true;
                    responseMsg = Encoding.UTF8.GetString(e.Data.Array, e.Data.Offset, e.Data.Count);
                };

                await client.StartAsync();
                await Task.Delay(500);

                await client.SendAsync("Test Message");
                await Task.Delay(1000);

                if (received && !string.IsNullOrEmpty(responseMsg))
                    Pass("WebSocket send/receive");
                else
                    Fail("WebSocket send/receive", "No response received");

                client.Stop();
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Fail("WebSocket send/receive", ex.Message);
            }
        }

        private async Task Test_WebSocket_Route()
        {
            try
            {
                _messageReceived = false;
                _receivedMessage = null;
                bool responseReceived = false;
                string response = null;

                var client = new WatsonWsClient("127.0.0.1", _testPort, false);
                client.MessageReceived += (sender, e) =>
                {
                    responseReceived = true;
                    response = Encoding.UTF8.GetString(e.Data.Array, e.Data.Offset, e.Data.Count);
                };

                await client.StartAsync();
                await Task.Delay(500);

                var message = new
                {
                    GUID = Guid.NewGuid(),
                    Route = "echo",
                    Data = Encoding.UTF8.GetBytes("Route Test")
                };
                string json = JsonSerializer.Serialize(message);
                await client.SendAsync(json);
                await Task.Delay(1000);

                if (_messageReceived && responseReceived && response.Contains("Echo"))
                    Pass("WebSocket route handling");
                else
                    Fail("WebSocket route handling", $"Message received: {_messageReceived}, Response: {responseReceived}");

                client.Stop();
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Fail("WebSocket route handling", ex.Message);
            }
        }

        private async Task Test_WebSocket_Disconnection()
        {
            try
            {
                _disconnectionReceived = false;

                var client = new WatsonWsClient("127.0.0.1", _testPort, false);
                await client.StartAsync();
                await Task.Delay(500);

                client.Stop();
                await Task.Delay(1000);

                if (_disconnectionReceived)
                    Pass("WebSocket disconnection");
                else
                    Fail("WebSocket disconnection", "Disconnection event not received");
            }
            catch (Exception ex)
            {
                Fail("WebSocket disconnection", ex.Message);
            }
        }
    }

    #endregion

    #region RabbitMQ Tests

    public class RabbitMqTests : TestSuite
    {
        public override string Name => "RabbitMQ Tests";
        private string _rabbitMqHost = "localhost";
        private bool _rabbitMqAvailable = false;

        private class ConnectionCheckResult
        {
            public bool Available { get; set; }
            public string ErrorMessage { get; set; }
        }

        protected override async Task RunTestsAsync()
        {
            // Check if RabbitMQ is available
            ConnectionCheckResult result = await CheckRabbitMqAvailable();
            _rabbitMqAvailable = result.Available;

            if (!_rabbitMqAvailable)
            {
                Console.WriteLine();
                Console.WriteLine($"RabbitMQ server not available on {_rabbitMqHost}");
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.WriteLine($"Connection error: {result.ErrorMessage}");
                }
                Console.WriteLine();
                Console.WriteLine("To run RabbitMQ tests, start a RabbitMQ server:");
                Console.WriteLine("  Docker:  docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management");
                Console.WriteLine("  Windows: Download from https://www.rabbitmq.com/download.html");
                Console.WriteLine("  Linux:   sudo apt-get install rabbitmq-server");
                Console.WriteLine();

                Skip("RabbitMQ broadcaster/receiver", "RabbitMQ server not available");
                Skip("RabbitMQ producer/consumer", "RabbitMQ server not available");
                Skip("RabbitMQ message correlation", "RabbitMQ server not available");
                Skip("RabbitMQ resilient broadcaster", "RabbitMQ server not available");
                return;
            }

            await Test_RabbitMq_BroadcasterReceiver();
            await Test_RabbitMq_ProducerConsumer();
            await Test_RabbitMq_MessageCorrelation();
        }

        private async Task<ConnectionCheckResult> CheckRabbitMqAvailable()
        {
            try
            {
                SwiftStackApp app = new SwiftStackApp("TestApp", quiet: true);
                app.LoggingSettings.EnableConsole = false;

                QueueProperties queueProps = new QueueProperties
                {
                    Hostname = _rabbitMqHost,
                    Name = "test-connection-check",
                    AutoDelete = true
                };

                RabbitMqBroadcaster<TestMessage> broadcaster = new RabbitMqBroadcaster<TestMessage>(
                    app.Serializer,
                    app.Logging,
                    queueProps,
                    1024 * 1024);

                await broadcaster.InitializeAsync();
                broadcaster.Dispose();
                return new ConnectionCheckResult { Available = true, ErrorMessage = null };
            }
            catch (Exception ex)
            {
                // Capture the root cause of the connection failure
                Exception innerEx = ex;
                while (innerEx.InnerException != null)
                    innerEx = innerEx.InnerException;

                string errorMessage = $"{innerEx.GetType().Name}: {innerEx.Message}";
                return new ConnectionCheckResult { Available = false, ErrorMessage = errorMessage };
            }
        }

        private async Task Test_RabbitMq_BroadcasterReceiver()
        {
            try
            {
                var app = new SwiftStackApp("TestApp", quiet: true);
                app.LoggingSettings.EnableConsole = false;

                var queueProps = new QueueProperties
                {
                    Hostname = _rabbitMqHost,
                    Name = "test-broadcast-" + Guid.NewGuid().ToString(),
                    AutoDelete = true
                };

                bool messageReceived = false;
                TestMessage receivedMessage = null;

                var broadcaster = new RabbitMqBroadcaster<TestMessage>(
                    app.Serializer, app.Logging, queueProps, 1024 * 1024);
                var receiver = new RabbitMqBroadcastReceiver<TestMessage>(
                    app.Serializer, app.Logging, queueProps);

                receiver.MessageReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        messageReceived = true;
                        receivedMessage = e.Data;
                    }
                };

                await broadcaster.InitializeAsync();
                await receiver.InitializeAsync();
                await Task.Delay(500);

                var testMsg = new TestMessage { Id = 1, Content = "Broadcast Test" };
                await broadcaster.Broadcast(testMsg, Guid.NewGuid().ToString());
                await Task.Delay(1500);

                if (messageReceived && receivedMessage != null && receivedMessage.Content == "Broadcast Test")
                    Pass("RabbitMQ broadcaster/receiver");
                else
                    Fail("RabbitMQ broadcaster/receiver", $"Message received: {messageReceived}");

                broadcaster.Dispose();
                receiver.Dispose();
            }
            catch (Exception ex)
            {
                Fail("RabbitMQ broadcaster/receiver", ex.Message);
            }
        }

        private async Task Test_RabbitMq_ProducerConsumer()
        {
            try
            {
                var app = new SwiftStackApp("TestApp", quiet: true);
                app.LoggingSettings.EnableConsole = false;

                var queueProps = new QueueProperties
                {
                    Hostname = _rabbitMqHost,
                    Name = "test-queue-" + Guid.NewGuid().ToString(),
                    AutoDelete = true
                };

                bool messageReceived = false;
                TestMessage receivedMessage = null;

                var producer = new RabbitMqProducer<TestMessage>(
                    app.Serializer, app.Logging, queueProps, 1024 * 1024);
                var consumer = new RabbitMqConsumer<TestMessage>(
                    app.Serializer, app.Logging, queueProps, autoAck: true);

                consumer.MessageReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        messageReceived = true;
                        receivedMessage = e.Data;
                    }
                };

                await producer.InitializeAsync();
                await consumer.InitializeAsync();
                await Task.Delay(500);

                var testMsg = new TestMessage { Id = 2, Content = "Queue Test" };
                await producer.SendMessage(testMsg, Guid.NewGuid().ToString());
                await Task.Delay(1500);

                if (messageReceived && receivedMessage != null && receivedMessage.Content == "Queue Test")
                    Pass("RabbitMQ producer/consumer");
                else
                    Fail("RabbitMQ producer/consumer", $"Message received: {messageReceived}");

                producer.Dispose();
                consumer.Dispose();
            }
            catch (Exception ex)
            {
                Fail("RabbitMQ producer/consumer", ex.Message);
            }
        }

        private async Task Test_RabbitMq_MessageCorrelation()
        {
            try
            {
                var app = new SwiftStackApp("TestApp", quiet: true);
                app.LoggingSettings.EnableConsole = false;

                var queueProps = new QueueProperties
                {
                    Hostname = _rabbitMqHost,
                    Name = "test-correlation-" + Guid.NewGuid().ToString(),
                    AutoDelete = true
                };

                string receivedCorrelationId = null;

                var broadcaster = new RabbitMqBroadcaster<TestMessage>(
                    app.Serializer, app.Logging, queueProps, 1024 * 1024);
                var receiver = new RabbitMqBroadcastReceiver<TestMessage>(
                    app.Serializer, app.Logging, queueProps);

                receiver.MessageReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        receivedCorrelationId = e.CorrelationId;
                    }
                };

                await broadcaster.InitializeAsync();
                await receiver.InitializeAsync();
                await Task.Delay(500);

                string expectedCorrelationId = Guid.NewGuid().ToString();
                var testMsg = new TestMessage { Id = 3, Content = "Correlation Test" };
                await broadcaster.Broadcast(testMsg, expectedCorrelationId);
                await Task.Delay(1500);

                if (receivedCorrelationId == expectedCorrelationId)
                    Pass("RabbitMQ message correlation");
                else
                    Fail("RabbitMQ message correlation",
                        $"Expected: {expectedCorrelationId}, Received: {receivedCorrelationId}");

                broadcaster.Dispose();
                receiver.Dispose();
            }
            catch (Exception ex)
            {
                Fail("RabbitMQ message correlation", ex.Message);
            }
        }

        private class TestMessage
        {
            public int Id { get; set; }
            public string Content { get; set; }
        }
    }

    #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
