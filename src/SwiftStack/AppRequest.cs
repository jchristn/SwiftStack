namespace SwiftStack
{
    using SerializationHelper;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using WatsonWebserver.Core;

    /// <summary>
    /// SwiftStack application request.
    /// </summary>
    public class AppRequest<T> where T : class
    {
        #region Public-Members

        /// <summary>
        /// Data to return after serialization.
        /// </summary>
        public T Data { get; set; } = null;

        /// <summary>
        /// HTTP context, if any.
        /// </summary>
        public HttpContextBase Http
        {
            get
            {
                return _Http;
            }
        }

        #endregion

        #region Private-Members

        private HttpContextBase _Http = null;
        private Serializer _Serializer = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// SwiftStack application request.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="isPrimitiveType">Boolean indicating if the request is using a primitive type.</param>
        public AppRequest(HttpContextBase ctx, Serializer serializer, bool isPrimitiveType)
        {
            _Http = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            if (!String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                if (isPrimitiveType)
                {
                    Data = (T)Convert.ChangeType(ctx.Request.DataAsString, typeof(T));
                }
                else
                {
                    Data = _Serializer.DeserializeJson<T>(ctx.Request.DataAsString);
                }
            }
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
