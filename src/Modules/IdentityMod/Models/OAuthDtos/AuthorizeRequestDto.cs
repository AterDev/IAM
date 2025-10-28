namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// OAuth/OIDC authorization request DTO
/// </summary>
public class AuthorizeRequestDto
{
    /// <summary>
    /// Response type (code, token, id_token)
    /// </summary>
    public required string ResponseType { get; set; }

    /// <summary>
    /// Client identifier
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// Redirect URI
    /// </summary>
    public required string RedirectUri { get; set; }

    /// <summary>
    /// Requested scopes (space-separated)
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// State parameter for CSRF protection
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// PKCE code challenge
    /// </summary>
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// PKCE code challenge method (plain, S256)
    /// </summary>
    public string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// Response mode (query, fragment, form_post)
    /// </summary>
    public string? ResponseMode { get; set; }

    /// <summary>
    /// Nonce for OIDC
    /// </summary>
    public string? Nonce { get; set; }

    /// <summary>
    /// Prompt parameter (none, login, consent, select_account)
    /// </summary>
    public string? Prompt { get; set; }
}
