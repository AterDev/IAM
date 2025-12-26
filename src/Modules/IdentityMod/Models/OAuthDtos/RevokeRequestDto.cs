using System.Text.Json.Serialization;

namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// Token revocation request DTO
/// </summary>
public class RevokeRequestDto
{
    /// <summary>
    /// Token to revoke
    /// </summary>
    [JsonPropertyName("token")]
    public required string Token { get; set; }

    /// <summary>
    /// Token type hint (access_token, refresh_token)
    /// </summary>
    [JsonPropertyName("token_type_hint")]
    public string? TokenTypeHint { get; set; }
}
