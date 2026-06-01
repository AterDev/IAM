/**
 * Request to regenerate recovery codes using a current TOTP code.
 */
export interface RegenerateRecoveryCodesRequestDto {
  /** Current TOTP code used to authorize regeneration. */
  code: string;
}
