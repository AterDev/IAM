namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// OAuth/OIDC token request DTO
/// </summary>
public class TokenRequestDto
{
    /// <summary>
    /// Grant type (authorization_code, refresh_token, client_credentials, password, device_code)
    /// </summary>
    public required string GrantType { get; set; }

    /// <summary>
    /// Client identifier
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Client secret
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Authorization code (for authorization_code grant)
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Redirect URI (for authorization_code grant)
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// PKCE code verifier
    /// </summary>
    public string? CodeVerifier { get; set; }

    /// <summary>
    /// Refresh token (for refresh_token grant)
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Requested scope
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Username (for password grant)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password (for password grant)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Device code (for device_code grant)
    /// </summary>
    public string? DeviceCode { get; set; }
}
