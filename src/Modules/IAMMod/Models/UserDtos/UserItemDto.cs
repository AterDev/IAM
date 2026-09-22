namespace IAMMod.Models.UserDtos;

/// <summary>
/// User item DTO for list display
/// </summary>
public class UserItemDto
{
    public Guid Id { get; set; }
    public required string UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool LockoutEnabled { get; set; }
    /// <summary>
    /// Account is currently locked when this timestamp is in the future.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}
