namespace IdentityMod.Models.UserDtos;

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
    public DateTimeOffset CreatedTime { get; set; }
}
