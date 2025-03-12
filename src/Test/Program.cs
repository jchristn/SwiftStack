namespace Test
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Metadata;
    using System.Threading;
    using System.Threading.Tasks;
    using SerializationHelper;
    using SwiftStack;

    public static class Program
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private static Serializer _Serializer = new Serializer();

        public static async Task Main(string[] args)
        {
            SwiftStackApp app = new SwiftStackApp();

            app.Get("/", async (req) => "Hello world");

            app.Post<string>("/loopback", async (req) => req.Data);

            app.Get("/search", async (req) =>
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
            });

            app.Get("/user", async (req) =>
            {
                return new 
                {
                    Email = "foo@bar.com",
                    Password = "password"
                };
            }); 
            
            app.Put<User>("/user/{id}", async (req) =>
            {
                string id = req.Parameters["id"];
                User user = req.GetData<User>();

                return new
                {
                    Id = id,
                    Email = user.Email,
                    Password = user.Password
                };
            });

            app.Get("/types/{type}", async (req) =>
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

            app.Get("/events/{count}", async (req) =>
            {
                int count = Convert.ToInt32(req.Parameters["count"].ToString());

                req.Http.Response.ServerSentEvents = true;

                for (int i = 0; i < count; i++)
                {
                    await req.Http.Response.SendEvent("Event " + i, false);
                    await Task.Delay(500);
                }

                await req.Http.Response.SendEvent(null, true);

                return null;
            });

            await app.Run();
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