/**
 * Recovery code payload returned when MFA is enabled or codes are regenerated.
 */
export interface MfaRecoveryCodesResponseDto {
  /** One-time recovery codes that must be stored by the user. */
  recoveryCodes: string[];
}
