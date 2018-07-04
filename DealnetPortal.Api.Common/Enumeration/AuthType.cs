namespace DealnetPortal.Api.Common.Enumeration
{
    /// <summary>
    /// For flexible client/service authentification configuration
    /// </summary>
    public enum AuthType
    {
        /// <summary>
        /// Use user registration with auto generated password send by email, and change password on fst logoin
        /// </summary>
        AuthProvider,
        /// <summary>
        /// Use classical login-password registration
        /// </summary>
        AuthProviderOneStepRegister,
        /// <summary>
        /// Stub for testing - login with any username/password
        /// </summary>
        Stub,
        /// <summary>
        /// Aspire authentification
        /// </summary>
        Aspire
    }
}
