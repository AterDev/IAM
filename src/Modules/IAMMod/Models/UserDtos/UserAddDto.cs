namespace IAMMod.Models.UserDtos;

/// <summary>
/// User add DTO
/// </summary>
public class UserAddDto
{
    [MaxLength(256)]
    public required string UserName { get; set; }

    [MaxLength(256)]
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [MaxLength(50)]
    [Phone]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public string? Password { get; set; }

    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool LockoutEnabled { get; set; } = true;
}
