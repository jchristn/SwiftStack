namespace SwiftStack
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Authentication and authorization result.
    /// </summary>
    public class AuthResult
    {
        #region Public-Members

        /// <summary>
        /// Authentication result.
        /// </summary>
        public AuthenticationResultEnum AuthenticationResult { get; set; } = AuthenticationResultEnum.Success;

        /// <summary>
        /// Authorization result.
        /// </summary>
        public AuthorizationResultEnum AuthorizationResult { get; set; } = AuthorizationResultEnum.Permitted;

        /// <summary>
        /// Metadata.
        /// </summary>
        public object Metadata { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Authentication and authorization result.
        /// </summary>
        public AuthResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
