/**
 * Decision payload for the authorize interaction.
 */
export interface AuthorizeInteractionDecisionDto {
  /** Requested client id. */
  clientId: string;
  /** Redirect URI from the authorization request. */
  redirectUri: string;
  /** OAuth response type. */
  responseType: string;
  /** Raw requested scopes. */
  scope?: string | null;
  /** Optional OAuth state. */
  state?: string | null;
  /** Optional OIDC nonce. */
  nonce?: string | null;
  /** Optional PKCE code challenge. */
  codeChallenge?: string | null;
  /** Optional PKCE code challenge method. */
  codeChallengeMethod?: string | null;
  /** Optional response mode. */
  responseMode?: string | null;
  /** Whether the user approved the request. */
  approve: boolean;
  /** Whether consent should be remembered permanently. */
  rememberConsent: boolean;
}
