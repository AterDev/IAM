namespace IAMMod.Models.UserDtos;

/// <summary>
/// User item DTO for list display
/// </summary>
public class UserItemDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool LockoutEnabled { get; set; }
    /// <summary>
    /// Account is currently locked when this timestamp is in the future.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset CreatedTime { get; set; }

    public static UserItemDto FromEntity(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        LockoutEnabled = user.LockoutEnabled,
        LockoutEnd = user.LockoutEnd,
        CreatedTime = user.CreatedTime,
    };
}
