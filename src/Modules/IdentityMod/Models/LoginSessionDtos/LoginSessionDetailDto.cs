namespace IdentityMod.Models.LoginSessionDtos;

/// <summary>
/// Login session detail DTO
/// </summary>
public class LoginSessionDetailDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string SessionId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceInfo { get; set; }
    public DateTimeOffset LoginTime { get; set; }
    public DateTimeOffset LastActivityTime { get; set; }
    public DateTimeOffset? ExpirationTime { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
