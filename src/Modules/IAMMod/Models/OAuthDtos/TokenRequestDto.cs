using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// OAuth/OIDC token request DTO
/// </summary>
public class TokenRequestDto
{
    /// <summary>
    /// Grant type (authorization_code, refresh_token, client_credentials, password, device_code)
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.GrantType)]
    public required string GrantType { get; set; }

    /// <summary>
    /// Client identifier
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.ClientId)]
    public string? ClientId { get; set; }

    /// <summary>
    /// Client secret
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.ClientSecret)]
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Authorization code (for authorization_code grant)
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.Code)]
    public string? Code { get; set; }

    /// <summary>
    /// Redirect URI (for authorization_code grant)
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.RedirectUri)]
    public string? RedirectUri { get; set; }

    /// <summary>
    /// PKCE code verifier
    /// </summary>
    [ModelBinder(Name = "code_verifier")]
    public string? CodeVerifier { get; set; }

    /// <summary>
    /// Refresh token (for refresh_token grant)
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.RefreshToken)]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Requested scope
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.Scope)]
    public string? Scope { get; set; }

    /// <summary>
    /// Username (for password grant)
    /// </summary>
    [ModelBinder(Name = "username")]
    public string? Username { get; set; }

    /// <summary>
    /// Password (for password grant)
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.Password)]
    public string? Password { get; set; }

    /// <summary>
    /// Device code (for device_code grant)
    /// </summary>
    [ModelBinder(Name = "device_code")]
    public string? DeviceCode { get; set; }

    /// <summary>
    /// Resource/API identifier (audience claim in token)
    /// </summary>
    [ModelBinder(Name = "resource")]
    public string? Resource { get; set; }

    /// <summary>
    /// Requested audience claim for the token
    /// </summary>
    [ModelBinder(Name = "audience")]
    public string? Audience { get; set; }
}
