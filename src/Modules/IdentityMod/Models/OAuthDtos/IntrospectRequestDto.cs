namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// Token introspection request DTO
/// </summary>
public class IntrospectRequestDto
{
    /// <summary>
    /// Token to introspect
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Token type hint (access_token, refresh_token)
    /// </summary>
    public string? TokenTypeHint { get; set; }
}
