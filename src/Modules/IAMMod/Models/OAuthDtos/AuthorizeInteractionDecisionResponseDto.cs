namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// Result returned after processing an authorize interaction decision.
/// </summary>
public class AuthorizeInteractionDecisionResponseDto
{
    /// <summary>
    /// Outcome status.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Redirect target for the calling client.
    /// </summary>
    public required string RedirectUrl { get; set; }

    /// <summary>
    /// Optional message for the caller.
    /// </summary>
    public string? Message { get; set; }
}