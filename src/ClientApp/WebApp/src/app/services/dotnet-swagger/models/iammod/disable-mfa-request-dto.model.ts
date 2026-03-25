/**
 * Request to disable MFA using a TOTP or recovery code.
 */
export interface DisableMfaRequestDto {
  /** Current TOTP or recovery code. */
  code: string;
}
