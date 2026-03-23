using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// Token revocation request DTO
/// </summary>
public class RevokeRequestDto
{
    /// <summary>
    /// Token to revoke
    /// </summary>
    [ModelBinder(Name = "token")]
    public required string Token { get; set; }

    /// <summary>
    /// Token type hint (access_token, refresh_token)
    /// </summary>
    [ModelBinder(Name = "token_type_hint")]
    public string? TokenTypeHint { get; set; }

    /// <summary>
    /// OAuth client id for authenticating the caller.
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.ClientId)]
    public string? ClientId { get; set; }

    /// <summary>
    /// OAuth client secret for authenticating the caller.
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.ClientSecret)]
    public string? ClientSecret { get; set; }
}
