namespace SwiftStack.RabbitMq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Incoming message.
    /// </summary>
    /// <typeparam name="T">Type of message.  Must be JSON serializable.</typeparam>
    public class IncomingMessage<T> where T : class
    {
        #region Public-Members

        /// <summary>
        /// Timestamp, in UTC time, when the message was sent.
        /// </summary>
        public DateTime TimestampUtc { get; } = DateTime.UtcNow;

        /// <summary>
        /// Delivery tag.
        /// </summary>
        public ulong DeliveryTag
        {
            get
            {
                return _DeliveryTag;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(DeliveryTag));
                _DeliveryTag = value;
            }
        }

        /// <summary>
        /// Correlation ID.
        /// </summary>
        public string CorrelationId { get; set; } = null;

        /// <summary>
        /// Timestamp, in UTC time, when this object should be considered expired.  Default is 7 days.
        /// </summary>
        public DateTime ExpirationUtc { get; set; } = DateTime.UtcNow.AddDays(7);

        /// <summary>
        /// Data from the message.
        /// </summary>
        public T Data { get; set; } = null;

        #endregion

        #region Private-Members

        private ulong _DeliveryTag = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public IncomingMessage()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
