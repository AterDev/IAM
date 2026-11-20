namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Self-service password change payload for the current authenticated user.
/// </summary>
public class ChangePasswordRequestDto
{
    [StringLength(100)]
    public required string CurrentPassword { get; set; }

    [StringLength(100)]
    public required string NewPassword { get; set; }
}