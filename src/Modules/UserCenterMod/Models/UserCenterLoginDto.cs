namespace UserCenterMod.Models;

/// <summary>
/// Password login request.
/// </summary>
public class UserCenterLoginDto
{
    public required string Email { get; set; }

    public required string Password { get; set; }
}
