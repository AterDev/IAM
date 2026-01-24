namespace Entity.IAMMod;

/// <summary>
/// OAuth 2.0 client types
/// </summary>
public enum ClientType
{
    /// <summary>
    /// Confidential client (can securely store credentials)
    /// </summary>
    Confidential,

    /// <summary>
    /// Public client (cannot securely store credentials)
    /// </summary>
    Public,
}

/// <summary>
/// OAuth 2.0 application types
/// </summary>
public enum ApplicationType
{
    /// <summary>
    /// Web application
    /// </summary>
    Web,

    /// <summary>
    /// Native/mobile application
    /// </summary>
    Native,

    /// <summary>
    /// Single page application (SPA)
    /// </summary>
    Spa,
}

/// <summary>
/// OAuth 2.0 consent prompt types
/// </summary>
public enum ConsentType
{
    /// <summary>
    /// Explicit consent required every time
    /// </summary>
    Explicit,

    /// <summary>
    /// Implicit consent (no user interaction required)
    /// </summary>
    Implicit,

    /// <summary>
    /// Systematic consent (implicit for future requests)
    /// </summary>
    Systematic,
}
