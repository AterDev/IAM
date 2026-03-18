namespace IAMMod.Models.UserDtos;

/// <summary>
/// Result of resolving an external login to a local user account.
/// </summary>
public class ExternalLoginResolutionResultDto
{
    /// <summary>
    /// Resolution status.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Local user id when resolution succeeds.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Local username when resolution succeeds.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Local email when resolution succeeds.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Whether a new local user account was created.
    /// </summary>
    public bool IsNewUser { get; set; }

    /// <summary>
    /// Optional provider name.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Optional message for diagnostics or UX.
    /// </summary>
    public string? Message { get; set; }
}