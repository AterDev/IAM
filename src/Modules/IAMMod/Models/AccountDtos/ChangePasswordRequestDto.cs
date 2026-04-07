namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Self-service password change payload for the current authenticated user.
/// </summary>
public class ChangePasswordRequestDto
{
    [MaxLength(100)]
    public required string CurrentPassword { get; set; }

    [MaxLength(100)]
    public required string NewPassword { get; set; }
}