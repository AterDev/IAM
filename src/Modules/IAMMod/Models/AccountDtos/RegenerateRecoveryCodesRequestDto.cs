namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Request to regenerate recovery codes using a current TOTP code.
/// </summary>
public class RegenerateRecoveryCodesRequestDto
{
    /// <summary>
    /// Current TOTP code used to authorize regeneration.
    /// </summary>
    public required string Code { get; set; }
}
