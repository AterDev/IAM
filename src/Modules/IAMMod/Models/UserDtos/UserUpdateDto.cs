namespace IAMMod.Models.UserDtos;

/// <summary>
/// User update DTO
/// </summary>
public class UserUpdateDto
{
    [StringLength(256)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(50)]
    [Phone]
    public string? PhoneNumber { get; set; }

    public bool? EmailConfirmed { get; set; }
    public bool? PhoneNumberConfirmed { get; set; }
    public bool? IsTwoFactorEnabled { get; set; }
    public bool? LockoutEnabled { get; set; }
}
