![alt tag](https://github.com/jchristn/swiftstack/blob/main/Assets/icon.ico?raw=true)

# SwiftStack

[![NuGet Version](https://img.shields.io/nuget/v/SwiftStack.svg?style=flat)](https://www.nuget.org/packages/SwiftStack/) [![NuGet](https://img.shields.io/nuget/dt/SwiftStack.svg)](https://www.nuget.org/packages/SwiftStack) 

SwiftStack is an opinionated and easy way to build REST APIs taking inspiration from elegant model shown in FastAPI in Python.

## New in v0.1.x

- Initial alpha release, all APIs subject to change

## Donations

If you would like to financially support my efforts, first of all, thank you!  Please refer to DONATIONS.md.

## Simple Example

```csharp
using SwiftStack;

SwiftStackApp app = new SwiftStackApp();

// Register a route with no request body
app.Route("GET", "/", async (req) =>
{
    return new AppResponse<string>
    {
        Data = "Hello world",
        Result = ApiResultEnum.Success
    };
});

// Register a route with a primitive request and response body            
app.Route<string, string>("POST", "/loopback", async (req) =>
{
    return new AppResponse<string>
    {
        Data = req.Data,
        Result = ApiResultEnum.Success
    };
});

// Register a route with a class request and response body
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
```

## Version History

Please refer to CHANGELOG.md for details.

## Logo

Thanks to [pngall.com](https://www.pngall.com/fast-png/download/92775/) for making this fantastic logo available.
