namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// Interaction context shown on the authorize page.
/// </summary>
public class AuthorizeInteractionContextDto
{
    /// <summary>
    /// Requested client id.
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// Client display name.
    /// </summary>
    public required string ClientName { get; set; }

    /// <summary>
    /// Optional client description.
    /// </summary>
    public string? ClientDescription { get; set; }

    /// <summary>
    /// Raw requested scopes.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Scope metadata for the interaction page.
    /// </summary>
    public List<OAuthInteractionScopeDto> RequestedScopes { get; set; } = [];

    /// <summary>
    /// Redirect URI from the authorization request.
    /// </summary>
    public required string RedirectUri { get; set; }

    /// <summary>
    /// OAuth response type.
    /// </summary>
    public required string ResponseType { get; set; }

    /// <summary>
    /// Optional OAuth state.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Optional OIDC nonce.
    /// </summary>
    public string? Nonce { get; set; }

    /// <summary>
    /// Optional PKCE code challenge.
    /// </summary>
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// Optional PKCE code challenge method.
    /// </summary>
    public string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// Optional response mode.
    /// </summary>
    public string? ResponseMode { get; set; }

    /// <summary>
    /// Current signed-in username.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Whether a matching valid consent already exists.
    /// </summary>
    public bool HasValidConsent { get; set; }
}