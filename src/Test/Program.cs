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

            app.Route("GET", "/", async (req) =>
            {
                return new AppResponse<string>
                {
                    Data = "Hello world",
                    Result = ApiResultEnum.Success
                };
            });

            app.Route<string, string>("POST", "/loopback", async (req) =>
            {
                return new AppResponse<string>
                {
                    Data = req.Data,
                    Result = ApiResultEnum.Success
                };
            });

            app.Route<User>("GET", "/user", async (req) =>
            {
                return new AppResponse<User>
                {
                    Data = new User { Email = "foo@bar.com", Password = "password" },
                    Pretty = false,
                    Result = ApiResultEnum.Success
                };
            });

            app.Route<User, User>("PUT", "/user/{id}", async (req) =>
            {
                string id = req.Http.Request.Url.Parameters.Get("id");
                return new AppResponse<User>
                {
                    Data = new User { Email = "user" + id + "@bar.com", Password = "password" },
                    Pretty = true,
                    Result = ApiResultEnum.Success
                };
            });

            await app.Run();
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}