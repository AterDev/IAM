namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Recovery code payload returned when MFA is enabled or codes are regenerated.
/// </summary>
public class MfaRecoveryCodesResponseDto
{
    /// <summary>
    /// One-time recovery codes that must be stored by the user.
    /// </summary>
    public List<string> RecoveryCodes { get; set; } = [];
}
