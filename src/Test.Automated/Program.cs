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
