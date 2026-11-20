namespace IAMMod.Models.LoginSessionDtos;

/// <summary>
/// Login session item DTO for list views
/// </summary>
public class LoginSessionItemDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string SessionId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset LoginTime { get; set; }
    public DateTimeOffset LastActivityTime { get; set; }
    public DateTimeOffset? ExpirationTime { get; set; }
    public bool IsActive { get; set; }

    public static LoginSessionItemDto FromEntity(LoginSession entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        SessionId = entity.SessionId,
        IpAddress = entity.IpAddress,
        UserAgent = entity.UserAgent,
        LoginTime = entity.LoginTime,
        LastActivityTime = entity.LastActivityTime,
        ExpirationTime = entity.ExpirationTime,
        IsActive = entity.IsActive,
    };
}
