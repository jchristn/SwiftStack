namespace SwiftStack.RabbitMq
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Message queue broadcast interface.
    /// </summary>
    /// <typeparam name="T">Type of message being sent.  Must be JSON serializable.</typeparam>
    public interface IMessageQueueBroadcaster<T> : IDisposable 
    {
        /// <summary>
        /// Broadcast a message.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <param name="correlationId">Correlation ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Output type.</returns>
        Task Broadcast(T msg, string correlationId, CancellationToken token = default);
    }
}
