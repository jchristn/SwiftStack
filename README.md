![alt tag](https://github.com/jchristn/swiftstack/blob/main/Assets/icon.ico?raw=true)

# SwiftStack

[![NuGet Version](https://img.shields.io/nuget/v/SwiftStack.svg?style=flat)](https://www.nuget.org/packages/SwiftStack/) [![NuGet](https://img.shields.io/nuget/dt/SwiftStack.svg)](https://www.nuget.org/packages/SwiftStack) 

SwiftStack is an opinionated and easy way to build REST APIs taking inspiration from elegant model shown in FastAPI in Python.

## New in v0.1.x

- Initial alpha release, all APIs subject to change

## Donations

If you would like to financially support my efforts, first of all, thank you!  Please refer to DONATIONS.md.

## Simple Example

Refer to the `Test` project and the `test.bat` batch file to test a simple example of SwiftStack.

```csharp
using SwiftStack;

SwiftStackApp app = new SwiftStackApp();

app.Route("GET", "/", async (req) =>
{
    return "Hello world";
});

app.Route("POST", "/loopback", async (req) =>
{
    return req.Data;
});

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

app.Put<User>("/user/{id}", async (req) =>
{
    string id = req.Parameters["id"];
    User user = req.GetData<User>();
    return new User
    {
        Id = id,
        Email = user.Email,
        Password = user.Password
    };
});

public class User
{
    public string Id { get; set; } = null;
    public string Email { get; set; } = null;
    public string Password { get; set; } = null;
}
```

## Version History

Please refer to CHANGELOG.md for details.

## Logo

Thanks to [pngall.com](https://www.pngall.com/fast-png/download/92775/) for making this fantastic logo available.
