namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Request to disable MFA using a TOTP or recovery code.
/// </summary>
public class DisableMfaRequestDto
{
    /// <summary>
    /// Current TOTP or recovery code.
    /// </summary>
    public required string Code { get; set; }
}
