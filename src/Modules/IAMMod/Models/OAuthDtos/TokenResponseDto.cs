using System.Text.Json.Serialization;

namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// OAuth/OIDC token response DTO
/// </summary>
public class TokenResponseDto
{
    /// <summary>
    /// Access token
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type (usually "Bearer")
    /// </summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    /// <summary>
    /// Expires in seconds
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    /// <summary>
    /// Refresh token
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// ID token (OIDC)
    /// </summary>
    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    /// <summary>
    /// Scope granted
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}
