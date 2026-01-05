using Microsoft.AspNetCore.Mvc;

namespace IdentityMod.Models.OAuthDtos;

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
}
