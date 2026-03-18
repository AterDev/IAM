using Microsoft.AspNetCore.Mvc;

namespace IAMMod.Models.OAuthDtos;

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

    /// <summary>
    /// OAuth client id for authenticating the caller.
    /// </summary>
    [ModelBinder(Name = "client_id")]
    public string? ClientId { get; set; }

    /// <summary>
    /// OAuth client secret for authenticating the caller.
    /// </summary>
    [ModelBinder(Name = "client_secret")]
    public string? ClientSecret { get; set; }
}
