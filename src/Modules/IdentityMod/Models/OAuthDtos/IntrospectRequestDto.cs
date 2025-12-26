using System.Text.Json.Serialization;

namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// Token introspection request DTO
/// </summary>
public class IntrospectRequestDto
{
    /// <summary>
    /// Token to introspect
    /// </summary>
    [JsonPropertyName("token")]
    public required string Token { get; set; }

    /// <summary>
    /// Token type hint (access_token, refresh_token)
    /// </summary>
    [JsonPropertyName("token_type_hint")]
    public string? TokenTypeHint { get; set; }
}
