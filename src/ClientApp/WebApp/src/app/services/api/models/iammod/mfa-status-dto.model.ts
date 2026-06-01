/**
 * Current MFA configuration status for the signed-in user.
 */
export interface MfaStatusDto {
  /** Whether MFA is currently enabled. */
  isEnabled: boolean;
  /** Whether there is a pending setup secret waiting to be verified. */
  hasPendingSetup: boolean;
  /** Number of remaining unused recovery codes. */
  recoveryCodesRemaining: number;
  /** Whether recovery codes can currently be regenerated. */
  canRegenerateRecoveryCodes: boolean;
}
