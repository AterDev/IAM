namespace IAMMod.Models.AccountDtos;

/// <summary>
/// MFA setup payload returned after generating a TOTP secret.
/// </summary>
public class MfaSetupResponseDto
{
    /// <summary>
    /// Manual entry secret in Base32 format.
    /// </summary>
    public required string Secret { get; set; }

    /// <summary>
    /// Standard otpauth URI for authenticator applications.
    /// </summary>
    public required string OtpAuthUri { get; set; }

    /// <summary>
    /// Display issuer name used in the authenticator app.
    /// </summary>
    public required string Issuer { get; set; }

    /// <summary>
    /// Account label used in the authenticator app.
    /// </summary>
    public required string AccountName { get; set; }
}
