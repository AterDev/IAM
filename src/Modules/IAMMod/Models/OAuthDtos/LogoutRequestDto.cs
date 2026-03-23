using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// Logout request DTO
/// </summary>
public class LogoutRequestDto
{
    /// <summary>
    /// ID token hint
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.IdTokenHint)]
    [JsonPropertyName(OpenIdConnectParameterNames.IdTokenHint)]
    public string? IdTokenHint { get; set; }

    /// <summary>
    /// Post logout redirect URI
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.PostLogoutRedirectUri)]
    [JsonPropertyName(OpenIdConnectParameterNames.PostLogoutRedirectUri)]
    public string? PostLogoutRedirectUri { get; set; }

    /// <summary>
    /// State parameter
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.State)]
    [JsonPropertyName(OpenIdConnectParameterNames.State)]
    public string? State { get; set; }
}
