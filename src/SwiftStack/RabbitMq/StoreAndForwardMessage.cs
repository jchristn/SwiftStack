namespace SwiftStack.RabbitMq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Store and forward message.
    /// </summary>
    public class StoreAndForwardMessage
    {
        #region Public-Members

        /// <summary>
        /// Correlation ID.
        /// </summary>
        public string CorrelationId { get; set; } = null;

        /// <summary>
        /// Delivery tag.
        /// </summary>
        public ulong DeliveryTag { get; set; } = 0;

        /// <summary>
        /// Enable or disable message queue persistence.
        /// </summary>
        public bool Persistent { get; set; } = false;

        /// <summary>
        /// Data.
        /// </summary>
        public byte[] Data { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Store and forward message.
        /// </summary>
        public StoreAndForwardMessage()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
