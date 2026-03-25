/**
 * Request to enable MFA using the current TOTP setup secret.
 */
export interface EnableMfaRequestDto {
  /** Current TOTP verification code. */
  code: string;
}
