namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// Logout request DTO
/// </summary>
public class LogoutRequestDto
{
    /// <summary>
    /// ID token hint
    /// </summary>
    public string? IdTokenHint { get; set; }

    /// <summary>
    /// Post logout redirect URI
    /// </summary>
    public string? PostLogoutRedirectUri { get; set; }

    /// <summary>
    /// State parameter
    /// </summary>
    public string? State { get; set; }
}
