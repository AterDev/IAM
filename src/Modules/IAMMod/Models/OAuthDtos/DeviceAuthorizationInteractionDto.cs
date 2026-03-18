namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// Interaction context shown on the device code verification page.
/// </summary>
public class DeviceAuthorizationInteractionDto
{
    /// <summary>
    /// Submitted user code.
    /// </summary>
    public required string UserCode { get; set; }

    /// <summary>
    /// Interaction status: pending, approved, denied, expired, invalid.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Optional message for the current status.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Client id associated with the device request.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Client display name.
    /// </summary>
    public string? ClientName { get; set; }

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
    /// Expiration time for the submitted user code.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the current interaction can still be approved.
    /// </summary>
    public bool CanApprove { get; set; }

    /// <summary>
    /// Whether the current interaction can still be denied.
    /// </summary>
    public bool CanDeny { get; set; }
}