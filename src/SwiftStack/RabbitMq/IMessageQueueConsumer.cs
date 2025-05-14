namespace SwiftStack.RabbitMq
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Message queue consumer interface.
    /// </summary>
    /// <typeparam name="T">Type of message being consumed.  Must be JSON serializable.</typeparam>
    public interface IMessageQueueConsumer<T> : IDisposable where T : class
    {
        /// <summary>
        /// Event handler fired when a message is received.
        /// </summary>
        event EventHandler<IncomingMessage<T>> MessageReceived;

        /// <summary>
        /// Acknowledge a message by correlation ID.
        /// </summary>
        /// <param name="deliveryTag">Delivery tag.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task Acknowledge(ulong deliveryTag, CancellationToken token = default);

        /// <summary>
        /// Reject a message by providing a non-acknowledgement (nack).
        /// </summary>
        /// <param name="deliveryTag">Delivery tag.</param>
        /// <param name="requeue">Boolean to indicate if the message should be requeued.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task Reject(ulong deliveryTag, bool requeue = true, CancellationToken token = default);
    }
}
