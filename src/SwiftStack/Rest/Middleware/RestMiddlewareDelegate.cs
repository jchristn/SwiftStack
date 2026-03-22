namespace SwiftStack.Rest.Middleware
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;

    /// <summary>
    /// Delegate for REST middleware.
    /// Middleware receives the HTTP context, a delegate to invoke the next middleware in the pipeline,
    /// and a cancellation token.
    /// Call next() to continue the pipeline, or return without calling next() to short-circuit.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="next">Delegate to invoke the next middleware or route handler.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Task.</returns>
    public delegate Task RestMiddlewareDelegate(HttpContextBase context, Func<Task> next, CancellationToken token);
}
