/**
 * OAuth/OIDC token request DTO
 */
export interface TokenRequestDto {
  /** Grant type (authorization_code, refresh_token, client_credentials, password, device_code) */
  grantType: string;
  /** Client identifier */
  clientId?: string | null;
  /** Client secret */
  clientSecret?: string | null;
  /** Authorization code (for authorization_code grant) */
  code?: string | null;
  /** Redirect URI (for authorization_code grant) */
  redirectUri?: string | null;
  /** PKCE code verifier */
  codeVerifier?: string | null;
  /** Refresh token (for refresh_token grant) */
  refreshToken?: string | null;
  /** Requested scope */
  scope?: string | null;
  /** Username (for password grant) */
  username?: string | null;
  /** Password (for password grant) */
  password?: string | null;
  /** Device code (for device_code grant) */
  deviceCode?: string | null;
  /** Resource/API identifier (audience claim in token) */
  resource?: string | null;
  /** Requested audience claim for the token */
  audience?: string | null;
}
