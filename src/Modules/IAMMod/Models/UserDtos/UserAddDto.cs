namespace IAMMod.Models.UserDtos;

/// <summary>
/// User add DTO
/// </summary>
public class UserAddDto
{
    [StringLength(256)]
    public required string UserName { get; set; }

    [StringLength(256)]
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [StringLength(50)]
    [Phone]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    public string? Password { get; set; }

    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool LockoutEnabled { get; set; } = true;
}
