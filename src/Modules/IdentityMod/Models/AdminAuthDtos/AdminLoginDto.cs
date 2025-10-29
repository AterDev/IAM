using System.ComponentModel.DataAnnotations;

namespace IdentityMod.Models.AdminAuthDtos;

/// <summary>
/// Admin login request DTO
/// </summary>
public class AdminLoginDto
{
    /// <summary>
    /// Username
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 4)]
    public required string UserName { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public required string Password { get; set; }
}
