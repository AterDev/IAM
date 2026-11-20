namespace IAMMod.Models.UserDtos;

/// <summary>
/// User detail DTO
/// </summary>
public class UserDetailDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }

    public static UserDetailDto FromEntity(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        EmailConfirmed = user.EmailConfirmed,
        PhoneNumber = user.PhoneNumber,
        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
        IsTwoFactorEnabled = user.IsTwoFactorEnabled,
        LockoutEnd = user.LockoutEnd,
        LockoutEnabled = user.LockoutEnabled,
        AccessFailedCount = user.AccessFailedCount,
        CreatedTime = user.CreatedTime,
        UpdatedTime = user.UpdatedTime,
    };
}
