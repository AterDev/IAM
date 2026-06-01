import { OAuthInteractionScopeDto } from '../iammod/oauth-interaction-scope-dto.model';

/**
 * Interaction context shown on the authorize page.
 */
export interface AuthorizeInteractionContextDto {
  /** Requested client id. */
  clientId: string;
  /** Client display name. */
  clientName: string;
  /** Optional client description. */
  clientDescription?: string | null;
  /** Raw requested scopes. */
  scope?: string | null;
  /** Scope metadata for the interaction page. */
  requestedScopes: OAuthInteractionScopeDto[];
  /** Redirect URI from the authorization request. */
  redirectUri: string;
  /** OAuth response type. */
  responseType: string;
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
  /** Current signed-in username. */
  userName?: string | null;
  /** Whether a matching valid consent already exists. */
  hasValidConsent: boolean;
}
