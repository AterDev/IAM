/**
 * OAuth/OIDC token response DTO
 */
export interface TokenResponseDto {
  /** Access token */
  access_token: string;
  /** Token type (usually "Bearer") */
  token_type?: string | null;
  /** Expires in seconds */
  expires_in?: number | null;
  /** Refresh token */
  refresh_token?: string | null;
  /** ID token (OIDC) */
  id_token?: string | null;
  /** Scope granted */
  scope?: string | null;
}
