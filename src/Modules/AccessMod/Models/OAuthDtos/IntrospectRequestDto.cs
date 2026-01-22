using Microsoft.AspNetCore.Mvc;

namespace AccessMod.Models.OAuthDtos;

/// <summary>
/// Token introspection request DTO
/// </summary>
public class IntrospectRequestDto
{
    /// <summary>
    /// Token to introspect
    /// </summary>
    [ModelBinder(Name = "token")]
    public required string Token { get; set; }

    /// <summary>
    /// Token type hint (access_token, refresh_token)
    /// </summary>
    [ModelBinder(Name = "token_type_hint")]
    public string? TokenTypeHint { get; set; }
}
