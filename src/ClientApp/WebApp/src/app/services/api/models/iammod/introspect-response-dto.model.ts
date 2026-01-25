/**
 * Token introspection response DTO
 */
export interface IntrospectResponseDto {
  /** Whether the token is active */
  active: boolean;
  /** Scope */
  scope?: string | null;
  /** Client ID */
  clientId?: string | null;
  /** Username */
  username?: string | null;
  /** Token type */
  tokenType?: string | null;
  /** Expiration time (Unix timestamp) */
  exp?: number | null;
  /** Issued at time (Unix timestamp) */
  iat?: number | null;
  /** Not before time (Unix timestamp) */
  nbf?: number | null;
  /** Subject */
  sub?: string | null;
  /** Audience */
  aud?: string | null;
  /** Issuer */
  iss?: string | null;
  /** JWT ID */
  jti?: string | null;
}
