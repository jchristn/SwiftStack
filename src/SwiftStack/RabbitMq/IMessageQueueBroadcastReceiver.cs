namespace SwiftStack.RabbitMq
{
    using System;

    /// <summary>
    /// Message queue broadcast receiver interface.
    /// </summary>
    /// <typeparam name="T">Type of message being received.  Must be JSON serializable.</typeparam>
    public interface IMessageQueueBroadcastReceiver<T> : IDisposable where T : class
    {
        /// <summary>
        /// Event handler fired when a broadcast message is received.
        /// </summary>
        event EventHandler<IncomingMessage<T>> MessageReceived;
    }
}
