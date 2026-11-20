/**
 * Logout request DTO
 */
export interface LogoutRequestDto {
  /** ID token hint */
  id_token_hint?: string | null;
  /** Post logout redirect URI */
  post_logout_redirect_uri?: string | null;
  /** State parameter */
  state?: string | null;
}
