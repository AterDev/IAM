namespace IdentityMod.Models;

/// <summary>
/// OAuth 2.0 grant types
/// </summary>
public enum GrantType
{
    /// <summary>
    /// Authorization code grant type
    /// </summary>
    AuthorizationCode,

    /// <summary>
    /// Refresh token grant type
    /// </summary>
    RefreshToken,

    /// <summary>
    /// Client credentials grant type
    /// </summary>
    ClientCredentials,

    /// <summary>
    /// Resource owner password credentials grant type
    /// </summary>
    Password,

    /// <summary>
    /// Device code grant type (RFC 8628)
    /// </summary>
    DeviceCode,
}

/// <summary>
/// OAuth 2.0 response types
/// </summary>
public enum ResponseType
{
    /// <summary>
    /// Authorization code flow
    /// </summary>
    Code,

    /// <summary>
    /// Implicit flow - access token
    /// </summary>
    Token,

    /// <summary>
    /// Implicit flow - ID token
    /// </summary>
    IdToken,
}

/// <summary>
/// OAuth 2.0 token types
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Access token
    /// </summary>
    AccessToken,

    /// <summary>
    /// Refresh token
    /// </summary>
    RefreshToken,

    /// <summary>
    /// ID token (OpenID Connect)
    /// </summary>
    IdToken,

    /// <summary>
    /// Authorization code
    /// </summary>
    AuthorizationCode,

    /// <summary>
    /// Device code
    /// </summary>
    DeviceCode,

    /// <summary>
    /// User code
    /// </summary>
    UserCode,
}

/// <summary>
/// OAuth 2.0 token status
/// </summary>
public enum TokenStatus
{
    /// <summary>
    /// Token is valid
    /// </summary>
    Valid,

    /// <summary>
    /// Token has been revoked
    /// </summary>
    Revoked,

    /// <summary>
    /// Token has been redeemed
    /// </summary>
    Redeemed,

    /// <summary>
    /// Token is pending
    /// </summary>
    Pending,

    /// <summary>
    /// Token was denied
    /// </summary>
    Denied,
}

/// <summary>
/// OAuth 2.0 authorization types
/// </summary>
public enum AuthorizationType
{
    /// <summary>
    /// Authorization code flow
    /// </summary>
    Code,

    /// <summary>
    /// Client credentials flow
    /// </summary>
    ClientCredentials,

    /// <summary>
    /// Password flow
    /// </summary>
    Password,

    /// <summary>
    /// Device code flow
    /// </summary>
    DeviceCode,
}

/// <summary>
/// OAuth 2.0 authorization status
/// </summary>
public enum AuthorizationStatus
{
    /// <summary>
    /// Authorization is valid
    /// </summary>
    Valid,

    /// <summary>
    /// Authorization has been revoked
    /// </summary>
    Revoked,

    /// <summary>
    /// Authorization is pending
    /// </summary>
    Pending,

    /// <summary>
    /// Authorization has been authorized
    /// </summary>
    Authorized,

    /// <summary>
    /// Authorization was denied
    /// </summary>
    Denied,
}

/// <summary>
/// PKCE code challenge methods
/// </summary>
public enum CodeChallengeMethod
{
    /// <summary>
    /// Plain text (not recommended for production)
    /// </summary>
    Plain,

    /// <summary>
    /// SHA-256 hash
    /// </summary>
    S256,
}

/// <summary>
/// OAuth 2.0 response modes
/// </summary>
public enum ResponseMode
{
    /// <summary>
    /// Query string
    /// </summary>
    Query,

    /// <summary>
    /// Fragment
    /// </summary>
    Fragment,

    /// <summary>
    /// Form post
    /// </summary>
    FormPost,
}
