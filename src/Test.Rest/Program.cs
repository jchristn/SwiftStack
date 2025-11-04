namespace Test.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Metadata;
    using System.Threading;
    using System.Threading.Tasks;
    using SerializationHelper;
    using SwiftStack;
    using SwiftStack.Rest;
    using WatsonWebserver.Core;

    public static class Program
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private static Serializer _Serializer = new Serializer();

        public static async Task Main(string[] args)
        {
            SwiftStackApp app = new SwiftStackApp("My test application", false);

            #region REST

            #region Unauthenticated-Routes

            app.Rest.Get("/", async (req) => "Hello, unauthenticated user");

            app.Rest.Get("/null-200", async (req) => null);

            app.Rest.Get("/null-204", async (req) =>
            {
                req.Http.Response.StatusCode = 204;
                return null;
            });

            app.Rest.Post<string>("/loopback", async (req) => req.Data);

            app.Rest.Get("/search", async (req) =>
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

            app.Rest.Get("/user", async (req) =>
            {
                return new 
                {
                    Email = "foo@bar.com",
                    Password = "password"
                };
            }); 
            
            app.Rest.Put<User>("/user/{id}", async (req) =>
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

            app.Rest.Get("/types/{type}", async (req) =>
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

            app.Rest.Get("/events/{count}", async (req) =>
            {
                int count = Convert.ToInt32(req.Parameters["count"].ToString());

                req.Http.Response.ServerSentEvents = true;

                for (int i = 0; i < count; i++)
                {
                    await req.Http.Response.SendEvent(
                        new ServerSentEvent 
                        {
                            Data = ("Event " + i)
                        }, 
                        false);
                    await Task.Delay(500);
                }

                await req.Http.Response.SendEvent(new ServerSentEvent { Data = "" }, true);

                return null;
            });

            app.Rest.Get("/exception/400", async (req) => 
            {
                throw new SwiftStackException(ApiResultEnum.BadRequest);
            });

            app.Rest.Get("/exception/401", async (req) =>
            {
                throw new SwiftStackException(ApiResultEnum.NotAuthorized);
            });

            app.Rest.Get("/exception/404", async (req) =>
            {
                throw new SwiftStackException(ApiResultEnum.NotFound);
            });

            app.Rest.Get("/exception/409", async (req) =>
            {
                throw new SwiftStackException(ApiResultEnum.Conflict);
            });

            app.Rest.Get("/exception/500", async (req) =>
            {
                throw new SwiftStackException(ApiResultEnum.InternalError);
            });

            #endregion

            #region Authenticated-Routes

            app.Rest.AuthenticationRoute = AuthenticationRoute;

            app.Rest.Get("/authenticated", async (req) => 
            {
                Console.WriteLine("HTTP context metadata: " + Environment.NewLine + _Serializer.SerializeJson(req.Http.Metadata, true));
                return "Hello, authenticated user";
            }, true);

            #endregion

            Task rest = Task.Run(() => app.Rest.Run());

            #endregion

            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }

        private static async Task<AuthResult> AuthenticationRoute(HttpContextBase ctx)
        {
            if (ctx.Request.Authorization != null)
            {
                if (!String.IsNullOrEmpty(ctx.Request.Authorization.Username)
                    && !String.IsNullOrEmpty(ctx.Request.Authorization.Password)
                    && ctx.Request.Authorization.Username.Equals("user")
                    && ctx.Request.Authorization.Password.Equals("password"))
                {
                    ctx.Metadata = new
                    {
                        Authorized = true,
                        Method = "credentials"
                    };

                    return new AuthResult
                    {
                        AuthenticationResult = AuthenticationResultEnum.Success,
                        AuthorizationResult = AuthorizationResultEnum.Permitted
                    };
                }
                else if (!String.IsNullOrEmpty(ctx.Request.Authorization.BearerToken)
                    && ctx.Request.Authorization.BearerToken.Equals("password"))
                {
                    ctx.Metadata = new
                    {
                        Authorized = true,
                        Method = "bearer"
                    };

                    return new AuthResult
                    {
                        AuthenticationResult = AuthenticationResultEnum.Success,
                        AuthorizationResult = AuthorizationResultEnum.Permitted
                    };
                }
            }

            return new AuthResult
            {
                AuthenticationResult = AuthenticationResultEnum.NotFound,
                AuthorizationResult = AuthorizationResultEnum.DeniedImplicit
            };
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