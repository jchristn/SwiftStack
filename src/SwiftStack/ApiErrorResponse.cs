namespace SwiftStack
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json.Serialization;

    /// <summary>
    /// API error response.
    /// </summary>
    public class ApiErrorResponse
    {
        #region Public-Members

        /// <summary>
        /// Status code.
        /// </summary>
        [JsonIgnore]
        public int StatusCode
        {
            get
            {
                return ApiResultEnumToStatusCode(Error);
            }
        }

        /// <summary>
        /// Error code.
        /// </summary>
        public ApiResultEnum Error { get; set; } = ApiResultEnum.NotFound;

        /// <summary>
        /// Description.
        /// </summary>
        public string Description
        {
            get
            {
                return ApiResultEnumToDescription(Error);
            }
        }

        /// <summary>
        /// User-supplied message.
        /// </summary>
        public string Message { get; set; } = null;

        /// <summary>
        /// User-supplied data.
        /// </summary>
        public object Data { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// API error response.
        /// </summary>
        public ApiErrorResponse()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        private string ApiResultEnumToDescription(ApiResultEnum result)
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

        private static int ApiResultEnumToStatusCode(ApiResultEnum result)
        {
            switch (result)
            {
                case ApiResultEnum.Success: return 200;
                case ApiResultEnum.Created: return 201;
                case ApiResultEnum.NotFound: return 404;
                case ApiResultEnum.NotAuthorized: return 401;
                case ApiResultEnum.InternalError: return 500;
                case ApiResultEnum.SlowDown: return 429;
                case ApiResultEnum.Conflict: return 409;
                case ApiResultEnum.BadRequest: return 400;
                case ApiResultEnum.DeserializationError: return 400;
                default: return 500;
            }
        }

        #endregion
    }
}
