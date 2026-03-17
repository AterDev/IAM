namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Password reset confirmation payload.
/// </summary>
public class ResetPasswordRequestDto
{
    [MaxLength(256)]
    [EmailAddress]
    public required string Email { get; set; }

    [MaxLength(32)]
    public required string Code { get; set; }

    [MaxLength(100)]
    public required string NewPassword { get; set; }
}