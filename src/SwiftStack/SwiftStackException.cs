namespace SwiftStack
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// SwiftStack exception.
    /// </summary>
    public class SwiftStackException : Exception
    {
        #region Public-Members

        /// <summary>
        /// Result.
        /// </summary>
        public ApiResultEnum Result { get; set; } = ApiResultEnum.NotFound;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// SwiftStack exception.
        /// </summary>
        /// <param name="result">Result.</param>
        public SwiftStackException(ApiResultEnum result) : base(GetDefaultMessage(result))
        {
            if (result == ApiResultEnum.Success) throw new ArgumentException("SwiftStack exceptions cannot be thrown with result Success.");
            if (result == ApiResultEnum.Created) throw new ArgumentException("SwiftStack exceptions cannot be thrown with result Created.");

            Result = result;
        }

        /// <summary>
        /// SwiftStack exception.
        /// </summary>
        /// <param name="result">Result.</param>
        /// <param name="message">Message.</param>
        public SwiftStackException(ApiResultEnum result, string message) : base(message)
        {
            Result = result;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        private static string GetDefaultMessage(ApiResultEnum result)
        {
            switch (result)
            {
                case ApiResultEnum.NotFound:
                    return "Resource not found";
                case ApiResultEnum.InternalError:
                    return "Internal server error";
                case ApiResultEnum.NotAuthorized:
                    return "Not authorized";
                case ApiResultEnum.SlowDown:
                    return "Too many requests";
                case ApiResultEnum.Conflict:
                    return "The requested operation would create an irresolvable conflict";
                default:
                    return "An error occurred";
            };
        }

        #endregion
    }
}
