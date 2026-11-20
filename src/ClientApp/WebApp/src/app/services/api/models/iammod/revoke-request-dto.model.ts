/**
 * Token revocation request DTO
 */
export interface RevokeRequestDto {
  /** Token to revoke */
  token: string;
  /** Token type hint (access_token, refresh_token) */
  tokenTypeHint?: string | null;
  /** OAuth client id for authenticating the caller. */
  clientId?: string | null;
  /** OAuth client secret for authenticating the caller. */
  clientSecret?: string | null;
}
