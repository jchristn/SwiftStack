﻿namespace SwiftStack
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
        public SwiftStackException(ApiResultEnum result) : base(ApiResultEnumToDescription(result))
        {
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

        private static string ApiResultEnumToDescription(ApiResultEnum result)
        {
            switch (result)
            {
                case ApiResultEnum.Success:
                    return "The operation completed successfully.";
                case ApiResultEnum.NotFound:
                    return "The requested resource was not found.";
                case ApiResultEnum.Created:
                    return "The resource was created successfully.";
                case ApiResultEnum.NotAuthorized:
                    return "You are not permitted to perform this action.";
                case ApiResultEnum.InternalError:
                    return "An internal error has occurred.";
                case ApiResultEnum.SlowDown:
                    return "The rate at which you are sending requests is too high.";
                case ApiResultEnum.Conflict:
                    return "The requested operation is not permitted as it would create a conflict.";
                case ApiResultEnum.BadRequest:
                    return "The request is invalid.  Please check your URL, headers, query, HTTP method, and request body.";
                case ApiResultEnum.DeserializationError:
                    return "The supplied object could not be deserialized.";
                default:
                    return "An API error of type " + result + " was encountered.";
            }
        }

        #endregion
    }
}
