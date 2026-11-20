namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Password reset request payload.
/// </summary>
public class ForgotPasswordRequestDto
{
    [StringLength(256)]
    [EmailAddress]
    public required string Email { get; set; }
}