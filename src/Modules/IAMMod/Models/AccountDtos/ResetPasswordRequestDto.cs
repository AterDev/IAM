namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Password reset confirmation payload.
/// </summary>
public class ResetPasswordRequestDto
{
    [StringLength(256)]
    [EmailAddress]
    public required string Email { get; set; }

    [StringLength(32)]
    public required string Code { get; set; }

    [StringLength(100)]
    public required string NewPassword { get; set; }
}