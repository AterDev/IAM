namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Password reset request payload.
/// </summary>
public class ForgotPasswordRequestDto
{
    [MaxLength(256)]
    [EmailAddress]
    public required string Email { get; set; }
}