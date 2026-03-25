/**
 * MFA setup payload returned after generating a TOTP secret.
 */
export interface MfaSetupResponseDto {
  /** Manual entry secret in Base32 format. */
  secret: string;
  /** Standard otpauth URI for authenticator applications. */
  otpAuthUri: string;
  /** Display issuer name used in the authenticator app. */
  issuer: string;
  /** Account label used in the authenticator app. */
  accountName: string;
}
