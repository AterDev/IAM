/**
 * OAuth/OIDC token response DTO
 */
export interface TokenResponseDto {
  /** Access token */
  accessToken?: string | null;
  /** Token type (usually "Bearer") */
  tokenType?: string | null;
  /** Expires in seconds */
  expiresIn?: number | null;
  /** Refresh token */
  refreshToken?: string | null;
  /** ID token (OIDC) */
  idToken?: string | null;
  /** Scope granted */
  scope?: string | null;
  /** Error code */
  error?: string | null;
  /** Error description */
  errorDescription?: string | null;
}
