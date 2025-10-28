namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// Device authorization response DTO
/// </summary>
public class DeviceAuthorizationResponseDto
{
    /// <summary>
    /// Device code
    /// </summary>
    public required string DeviceCode { get; set; }

    /// <summary>
    /// User code
    /// </summary>
    public required string UserCode { get; set; }

    /// <summary>
    /// Verification URI
    /// </summary>
    public required string VerificationUri { get; set; }

    /// <summary>
    /// Verification URI complete (optional)
    /// </summary>
    public string? VerificationUriComplete { get; set; }

    /// <summary>
    /// Expires in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Interval for polling in seconds
    /// </summary>
    public int Interval { get; set; }
}
