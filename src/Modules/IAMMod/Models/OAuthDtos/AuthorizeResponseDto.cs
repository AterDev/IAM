using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text.Json.Serialization;

namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// OAuth/OIDC authorization response DTO
/// </summary>
public class AuthorizeResponseDto
{
    /// <summary>
    /// Authorization code
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.Code)]
    public string? Code { get; set; }

    /// <summary>
    /// Access token (for implicit flow)
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.AccessToken)]
    public string? AccessToken { get; set; }

    /// <summary>
    /// Token type
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.TokenType)]
    public string? TokenType { get; set; }

    /// <summary>
    /// Expires in seconds
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.ExpiresIn)]
    public int? ExpiresIn { get; set; }

    /// <summary>
    /// ID token (for OIDC)
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.IdToken)]
    public string? IdToken { get; set; }

    /// <summary>
    /// State parameter
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.State)]
    public string? State { get; set; }

    /// <summary>
    /// Scope granted
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.Scope)]
    public string? Scope { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.Error)]
    public string? Error { get; set; }

    /// <summary>
    /// Error description
    /// </summary>
    [JsonPropertyName(OpenIdConnectParameterNames.ErrorDescription)]
    public string? ErrorDescription { get; set; }
}
