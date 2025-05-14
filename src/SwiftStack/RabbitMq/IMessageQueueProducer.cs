namespace SwiftStack.RabbitMq
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Message queue producer interface.
    /// </summary>
    /// <typeparam name="T">Type of message being produced.  Must be JSON serializable.</typeparam>
    public interface IMessageQueueProducer<T> : IDisposable where T : class
    {
        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <param name="correlationId">Correlation ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessage(T msg, string correlationId, CancellationToken token = default);

        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <param name="persist">True to enable persistence.</param>
        /// <param name="correlationId">Correlation ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessage(T msg, bool persist, string correlationId, CancellationToken token = default);
    }
}
