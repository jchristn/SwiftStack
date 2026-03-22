# Change Log

## Current Version

v0.4.8

- **Middleware pipeline** for composable request processing via `app.Rest.Use()`
- **Request timeout support** via `app.Rest.UseTimeout()` with automatic 408 responses
- **Health check endpoints** via `app.Rest.UseHealthCheck()` with Healthy/Degraded/Unhealthy status
- **CancellationToken on AppRequest** for cooperative cancellation in route handlers
- **RequestTimeout** added to `ApiResultEnum` (HTTP 408)
- **Disposal fixes**: removed `async void` from Dispose methods, added timeouts to blocking cleanup, null-safe Dispose before Run()
- **Resource leak fixes**: `RabbitMqApp.Dispose()` now disposes managed children, `WebsocketsApp.Dispose()` clears all event handlers
- **Test suite expanded** from 69 to 102 tests with per-test timing

## Previous Versions

v0.4.6

- **Default route support** for custom catch-all handling of unmatched requests
- **OpenAPI 3.0 and Swagger UI support** for REST APIs

v0.3.x

- **WebSockets application support** via `WebsocketsApp`
- **RabbitMQ resilient interfaces** with on-disk indexing for recovery

v0.2.0

- Collapse REST methods into `RestApp`

v0.1.0

- Initial alpha
