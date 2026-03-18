namespace IAMMod.Models.AccountDtos;

/// <summary>
/// Current MFA configuration status for the signed-in user.
/// </summary>
public class MfaStatusDto
{
    /// <summary>
    /// Whether MFA is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Whether there is a pending setup secret waiting to be verified.
    /// </summary>
    public bool HasPendingSetup { get; set; }

    /// <summary>
    /// Number of remaining unused recovery codes.
    /// </summary>
    public int RecoveryCodesRemaining { get; set; }

    /// <summary>
    /// Whether recovery codes can currently be regenerated.
    /// </summary>
    public bool CanRegenerateRecoveryCodes => IsEnabled;
}
