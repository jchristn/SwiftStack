namespace SwiftStack
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
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
        /// Get as long integer.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Long integer.</returns>
        public long GetLong(string name, long defaultValue = 0)
        {
            string value = _Parameters[name];
            if (String.IsNullOrEmpty(value)) return defaultValue;
            if (long.TryParse(value, out long result)) return result;
            return defaultValue;
        }

        /// <summary>
        /// Get as double.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Double.</returns>
        public double GetDouble(string name, double defaultValue = 0.0)
        {
            string value = _Parameters[name];
            if (String.IsNullOrEmpty(value)) return defaultValue;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)) return result;
            return defaultValue;
        }

        /// <summary>
        /// Get as decimal.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Decimal.</returns>
        public decimal GetDecimal(string name, decimal defaultValue = 0m)
        {
            string value = _Parameters[name];
            if (String.IsNullOrEmpty(value)) return defaultValue;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result)) return result;
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

            // Additional boolean support for common string values
            value = value.ToLower();
            if (value == "1" || value == "yes" || value == "y" || value == "on") return true;
            if (value == "0" || value == "no" || value == "n" || value == "off") return false;

            return defaultValue;
        }

        /// <summary>
        /// Get as DateTime.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>DateTime.</returns>
        public DateTime GetDateTime(string name, DateTime? defaultValue = null)
        {
            string value = _Parameters[name];
            if (String.IsNullOrEmpty(value)) return defaultValue ?? DateTime.MinValue;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result)) return result;
            return defaultValue ?? DateTime.MinValue;
        }

        /// <summary>
        /// Get as TimeSpan.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>TimeSpan.</returns>
        public TimeSpan GetTimeSpan(string name, TimeSpan? defaultValue = null)
        {
            string value = _Parameters[name];
            if (String.IsNullOrEmpty(value)) return defaultValue ?? TimeSpan.Zero;
            if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan result)) return result;
            return defaultValue ?? TimeSpan.Zero;
        }

        /// <summary>
        /// Get as Guid.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Guid.</returns>
        public Guid GetGuid(string name, Guid? defaultValue = null)
        {
            string value = _Parameters[name];
            if (String.IsNullOrEmpty(value)) return defaultValue ?? Guid.Empty;
            if (Guid.TryParse(value, out Guid result)) return result;
            return defaultValue ?? Guid.Empty;
        }

        /// <summary>
        /// Get as enum.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <param name="ignoreCase">Ignore case when parsing.</param>
        /// <returns>Enum value.</returns>
        public T GetEnum<T>(string name, T defaultValue, bool ignoreCase = true) where T : struct, Enum
        {
            string value = _Parameters[name];
            if (String.IsNullOrEmpty(value)) return defaultValue;
            if (Enum.TryParse<T>(value, ignoreCase, out T result)) return result;
            return defaultValue;
        }

        /// <summary>
        /// Get as array of strings (comma-separated).
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="separator">Separator character.  Default is comma.</param>
        /// <returns>String array.</returns>
        public string[] GetArray(string name, char separator = ',')
        {
            string value = _Parameters[name];
            if (String.IsNullOrEmpty(value)) return Array.Empty<string>();
            return value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
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

        /// <summary>
        /// Try to get a value as a specific type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="name">Parameter name.</param>
        /// <param name="result">The result if successful.</param>
        /// <returns>True if the parameter exists and was successfully converted.</returns>
        public bool TryGetValue<T>(string name, out T result)
        {
            result = default;
            string value = _Parameters[name];
            if (String.IsNullOrEmpty(value)) return false;

            try
            {
                if (typeof(T) == typeof(string))
                {
                    result = (T)(object)value;
                    return true;
                }
                else if (typeof(T) == typeof(int))
                {
                    if (int.TryParse(value, out int intResult))
                    {
                        result = (T)(object)intResult;
                        return true;
                    }
                }
                else if (typeof(T) == typeof(bool))
                {
                    if (bool.TryParse(value, out bool boolResult))
                    {
                        result = (T)(object)boolResult;
                        return true;
                    }

                    // Additional boolean support
                    value = value.ToLower();
                    if (value == "1" || value == "yes" || value == "y" || value == "on")
                    {
                        result = (T)(object)true;
                        return true;
                    }
                    if (value == "0" || value == "no" || value == "n" || value == "off")
                    {
                        result = (T)(object)false;
                        return true;
                    }
                }
                else if (typeof(T) == typeof(double))
                {
                    if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleResult))
                    {
                        result = (T)(object)doubleResult;
                        return true;
                    }
                }
                else if (typeof(T) == typeof(decimal))
                {
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decimalResult))
                    {
                        result = (T)(object)decimalResult;
                        return true;
                    }
                }
                else if (typeof(T) == typeof(DateTime))
                {
                    if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateResult))
                    {
                        result = (T)(object)dateResult;
                        return true;
                    }
                }
                else if (typeof(T) == typeof(Guid))
                {
                    if (Guid.TryParse(value, out Guid guidResult))
                    {
                        result = (T)(object)guidResult;
                        return true;
                    }
                }
                else if (typeof(T).IsEnum)
                {
                    try
                    {
                        result = (T)Enum.Parse(typeof(T), value, true);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    // Try using Convert as a fallback
                    result = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch
            {
                // Conversion failed
                return false;
            }

            return false;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}