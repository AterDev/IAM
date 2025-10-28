namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// Token revocation request DTO
/// </summary>
public class RevokeRequestDto
{
    /// <summary>
    /// Token to revoke
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Token type hint (access_token, refresh_token)
    /// </summary>
    public string? TokenTypeHint { get; set; }
}
