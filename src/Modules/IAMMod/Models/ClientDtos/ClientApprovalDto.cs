namespace IAMMod.Models.ClientDtos;

/// <summary>
/// Review payload for approving a pending client registration.
/// </summary>
public class ClientApprovalDto
{
    /// <summary>
    /// Secret validity period in days.
    /// </summary>
    public int SecretExpirationDays { get; set; } = 180;
}