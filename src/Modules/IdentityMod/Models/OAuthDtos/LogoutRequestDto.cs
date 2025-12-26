using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// Logout request DTO
/// </summary>
public class LogoutRequestDto
{
    /// <summary>
    /// ID token hint
    /// </summary>
    [ModelBinder(Name = "id_token_hint")]
    [JsonPropertyName("id_token_hint")]
    public string? IdTokenHint { get; set; }

    /// <summary>
    /// Post logout redirect URI
    /// </summary>
    [ModelBinder(Name = "post_logout_redirect_uri")]
    [JsonPropertyName("post_logout_redirect_uri")]
    public string? PostLogoutRedirectUri { get; set; }

    /// <summary>
    /// State parameter
    /// </summary>
    [ModelBinder(Name = "state")]
    [JsonPropertyName("state")]
    public string? State { get; set; }
}
