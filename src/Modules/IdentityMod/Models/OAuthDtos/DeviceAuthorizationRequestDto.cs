namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// Device authorization request DTO
/// </summary>
public class DeviceAuthorizationRequestDto
{
    /// <summary>
    /// Client identifier
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// Requested scope
    /// </summary>
    public string? Scope { get; set; }
}
