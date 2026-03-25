import { OAuthInteractionScopeDto } from '../iammod/oauth-interaction-scope-dto.model';

/**
 * Interaction context shown on the device code verification page.
 */
export interface DeviceAuthorizationInteractionDto {
  /** Submitted user code. */
  userCode: string;
  /** Interaction status: pending, approved, denied, expired, invalid. */
  status: string;
  /** Optional message for the current status. */
  message?: string | null;
  /** Client id associated with the device request. */
  clientId?: string | null;
  /** Client display name. */
  clientName?: string | null;
  /** Optional client description. */
  clientDescription?: string | null;
  /** Raw requested scopes. */
  scope?: string | null;
  /** Scope metadata for the interaction page. */
  requestedScopes: OAuthInteractionScopeDto[];
  /** Expiration time for the submitted user code. */
  expiresAt?: Date | null;
  /** Whether the current interaction can still be approved. */
  canApprove: boolean;
  /** Whether the current interaction can still be denied. */
  canDeny: boolean;
}
