namespace SwiftStack
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Runtime.InteropServices.ComTypes;
    using System.Text;

    /// <summary>
    /// SwiftStack application response.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    public class AppResponse<T> : AppResponse
    {
        /// <summary>
        /// Data to be returned after serialization.
        /// </summary>
        public new T Data { get; set; } = default;

        /// <summary>
        /// Boolean to enable or disable pretty print during serialization.
        /// </summary>
        public bool Pretty { get; set; } = true;
    }

    /// <summary>
    /// SwiftStack application response.
    /// </summary>
    public class AppResponse
    {
        #region Public-Members

        /// <summary>
        /// Data.
        /// </summary>
        public virtual object Data { get; set; } = null;

        /// <summary>
        /// Result.
        /// </summary>
        public ApiResultEnum Result { get; set; } = ApiResultEnum.Success;

        /// <summary>
        /// Status code.
        /// </summary>
        public int? StatusCode
        {
            get
            {
                return _StatusCode;
            }
            set
            {
                if (value != null)
                    if (value.Value < 100 || value.Value > 599) throw new ArgumentOutOfRangeException(nameof(StatusCode));

                _StatusCode = value;
            }
        }

        /// <summary>
        /// Headers.
        /// </summary>
        public NameValueCollection Headers
        {
            get
            {
                return _Headers;
            }
            set
            {
                if (value == null) value = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
                _Headers = value;
            }
        }

        #endregion

        #region Private-Members

        private int? _StatusCode = 200;
        private NameValueCollection _Headers { get; set; } = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// SwiftStack application response.
        /// </summary>
        public AppResponse()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
