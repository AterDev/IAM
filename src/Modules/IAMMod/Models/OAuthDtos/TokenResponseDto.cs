using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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
    [JsonPropertyName(OpenIdConnectParameterNames.AccessToken)]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type (usually "Bearer")
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.TokenType)]
    public string? TokenType { get; set; }

    /// <summary>
    /// Expires in seconds
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.ExpiresIn)]
    public int? ExpiresIn { get; set; }

    /// <summary>
    /// Refresh token
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.RefreshToken)]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// ID token (OIDC)
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.IdToken)]
    public string? IdToken { get; set; }

    /// <summary>
    /// Scope granted
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.Scope)]
    public string? Scope { get; set; }
}
