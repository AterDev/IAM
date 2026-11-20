/**
 * Token introspection request DTO
 */
export interface IntrospectRequestDto {
  /** Token to introspect */
  token: string;
  /** Token type hint (access_token, refresh_token) */
  tokenTypeHint?: string | null;
  /** OAuth client id for authenticating the caller. */
  clientId?: string | null;
  /** OAuth client secret for authenticating the caller. */
  clientSecret?: string | null;
}
