namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Public self-service registration request.
/// </summary>
public class RegisterRequestDto
{
    [MaxLength(256)]
    public required string UserName { get; set; }

    [MaxLength(256)]
    [EmailAddress]
    public required string Email { get; set; }

    [MaxLength(50)]
    [Phone]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public required string Password { get; set; }
}