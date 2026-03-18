namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// Decision payload for a device authorization interaction.
/// </summary>
public class DeviceAuthorizationDecisionDto
{
    /// <summary>
    /// Submitted user code.
    /// </summary>
    public required string UserCode { get; set; }

    /// <summary>
    /// Whether the device request is approved.
    /// </summary>
    public bool Approve { get; set; }
}