using System.ComponentModel.DataAnnotations;

namespace IAMMod.Models.AdminAuthDtos;

/// <summary>
/// Admin login request DTO
/// </summary>
public class AdminLoginDto
{
    /// <summary>
    /// Email address used for login
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256, MinimumLength = 4)]
    public required string Email { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public required string Password { get; set; }
}
