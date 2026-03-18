namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// Decision payload for the authorize interaction.
/// </summary>
public class AuthorizeInteractionDecisionDto
{
    /// <summary>
    /// Requested client id.
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// Redirect URI from the authorization request.
    /// </summary>
    public required string RedirectUri { get; set; }

    /// <summary>
    /// OAuth response type.
    /// </summary>
    public required string ResponseType { get; set; }

    /// <summary>
    /// Raw requested scopes.
    /// </summary>
    public string? Scope { get; set; }

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
    /// Whether the user approved the request.
    /// </summary>
    public bool Approve { get; set; }

    /// <summary>
    /// Whether consent should be remembered permanently.
    /// </summary>
    public bool RememberConsent { get; set; }
}