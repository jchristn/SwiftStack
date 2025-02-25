namespace SwiftStack
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;

    /// <summary>
    /// Request parameters.
    /// </summary>
    public class RequestParameters
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly NameValueCollection _Parameters;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Request parameters.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        public RequestParameters(NameValueCollection parameters)
        {
            _Parameters = parameters ?? new NameValueCollection();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Get parameter by name.
        /// </summary>
        /// <param name="name">Name.</param>
        public string this[string name]
        {
            get { return _Parameters[name]; }
        }

        /// <summary>
        /// Get as integer.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Integer.</returns>
        public int GetInt(string name, int defaultValue = 0)
        {
            string value = _Parameters[name];
            if (String.IsNullOrEmpty(value)) return defaultValue;
            if (int.TryParse(value, out int result)) return result;
            return defaultValue;
        }

        /// <summary>
        /// Get with default value.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>String.</returns>
        public string GetValueOrDefault(string name, string defaultValue = null)
        {
            string value = _Parameters[name];
            return String.IsNullOrEmpty(value) ? defaultValue : value;
        }

        /// <summary>
        /// Get as boolean.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Boolean.</returns>
        public bool GetBool(string name, bool defaultValue = false)
        {
            string value = _Parameters[name];
            if (String.IsNullOrEmpty(value)) return defaultValue;
            if (bool.TryParse(value, out bool result)) return result;
            return defaultValue;
        }

        /// <summary>
        /// Get all parameter keys.
        /// </summary>
        /// <returns>Keys.</returns>
        public string[] GetKeys()
        {
            return _Parameters.AllKeys;
        }

        /// <summary>
        /// Check if parameter exists.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <returns>True if exists.</returns>
        public bool Contains(string name)
        {
            return _Parameters[name] != null;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
