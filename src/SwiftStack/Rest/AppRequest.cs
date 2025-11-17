namespace SwiftStack.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using SwiftStack.Serialization;
    using WatsonWebserver.Core;

    /// <summary>
    /// Application request.
    /// </summary>
    public class AppRequest
    {
        #region Public-Members

        /// <summary>
        /// The deserialized request data.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// HTTP context.
        /// </summary>
        public HttpContextBase Http { get; }

        /// <summary>
        /// Parameters from the route URL.
        /// </summary>
        public RequestParameters Parameters { get; }

        /// <summary>
        /// Query parameters.
        /// </summary>
        public RequestParameters Query { get; }

        /// <summary>
        /// Headers from the request.
        /// </summary>
        public RequestParameters Headers { get; }

        /// <summary>
        /// Serializer instance.
        /// </summary>
        public ISerializer Serializer { get; }

        /// <summary>
        /// Authentication and authorization result.
        /// </summary>
        public AuthResult AuthResult { get; set; }

        /// <summary>
        /// User-supplied metadata.
        /// </summary>
        public object Metadata { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Application request.
        /// </summary>
        public AppRequest(HttpContextBase ctx, ISerializer serializer, object data)
        {
            Http = ctx ?? throw new ArgumentNullException(nameof(ctx));
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Data = data;

            Parameters = new RequestParameters(ctx.Request.Url.Parameters);
            Query = new RequestParameters(ctx.Request.Query.Elements);
            Headers = new RequestParameters(ctx.Request.Headers);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Cast data to a specific type.
        /// </summary>
        public T GetData<T>() where T : class
        {
            return Data as T;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}