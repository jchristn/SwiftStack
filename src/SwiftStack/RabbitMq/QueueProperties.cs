namespace SwiftStack.RabbitMq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Queue properties.
    /// </summary>
    public class QueueProperties
    {
        #region Public-Members

        /// <summary>
        /// Hostname.
        /// </summary>
        public string Hostname
        {
            get
            {
                return _Hostname;
            }
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Hostname));
                _Hostname = value;
            }
        }

        /// <summary>
        /// Channel name.
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Name));
                _Name = value;
            }
        }

        /// <summary>
        /// Enable or disable durability of the queue.  
        /// This allows the queue to survive broker restarts by persisting to disk.
        /// </summary>
        public bool Durable { get; set; } = false;

        /// <summary>
        /// Enable or disable exclusive use of the queue.  
        /// When enabled, the queue can only be used by one connection, will be deleted when that connection closes, and no other connection can consume from the queue.
        /// </summary>
        public bool Exclusive { get; set; } = false;

        /// <summary>
        /// Enable or disable auto-deletion.
        /// When enabled, the queue will be deleted when all consumers have unsubscribed.
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// Fully qualified name, e.g. 'hostname/channel'.
        /// </summary>
        public string FullyQualifiedName
        {
            get
            {
                return Hostname + "/" + Name;
            }
        }

        #endregion

        #region Private-Members

        private string _Hostname = "localhost";
        private string _Name = "channel";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public QueueProperties()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
