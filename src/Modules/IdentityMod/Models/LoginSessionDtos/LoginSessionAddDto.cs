namespace IdentityMod.Models.LoginSessionDtos;

/// <summary>
/// Login session add DTO
/// </summary>
public class LoginSessionAddDto
{
    public Guid UserId { get; set; }
    public required string SessionId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceInfo { get; set; }
    public DateTimeOffset? ExpirationTime { get; set; }
}
