namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Public self-service registration request.
/// </summary>
public class RegisterRequestDto
{
    [StringLength(256)]
    public required string UserName { get; set; }

    [StringLength(256)]
    [EmailAddress]
    public required string Email { get; set; }

    [StringLength(50)]
    [Phone]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    public required string Password { get; set; }
}