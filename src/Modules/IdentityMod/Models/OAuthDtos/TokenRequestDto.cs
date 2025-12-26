using System.Text.Json.Serialization;

namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// OAuth/OIDC token request DTO
/// </summary>
public class TokenRequestDto
{
    /// <summary>
    /// Grant type (authorization_code, refresh_token, client_credentials, password, device_code)
    /// </summary>
    [JsonPropertyName("grant_type")]
    public required string GrantType { get; set; }

    /// <summary>
    /// Client identifier
    /// </summary>
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    /// <summary>
    /// Client secret
    /// </summary>
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Authorization code (for authorization_code grant)
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Redirect URI (for authorization_code grant)
    /// </summary>
    [JsonPropertyName("redirect_uri")]
    public string? RedirectUri { get; set; }

    /// <summary>
    /// PKCE code verifier
    /// </summary>
    [JsonPropertyName("code_verifier")]
    public string? CodeVerifier { get; set; }

    /// <summary>
    /// Refresh token (for refresh_token grant)
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Requested scope
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// Username (for password grant)
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Password (for password grant)
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// Device code (for device_code grant)
    /// </summary>
    [JsonPropertyName("device_code")]
    public string? DeviceCode { get; set; }
}
