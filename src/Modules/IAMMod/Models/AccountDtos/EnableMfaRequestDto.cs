namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Request to enable MFA using the current TOTP setup secret.
/// </summary>
public class EnableMfaRequestDto
{
    /// <summary>
    /// Current TOTP verification code.
    /// </summary>
    public required string Code { get; set; }
}
