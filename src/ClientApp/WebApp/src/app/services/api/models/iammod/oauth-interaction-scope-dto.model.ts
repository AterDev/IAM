/**
 * Scope metadata for OAuth interaction pages.
 */
export interface OAuthInteractionScopeDto {
  /** Scope name. */
  name: string;
  /** User-facing scope display name. */
  displayName: string;
  /** Optional scope description. */
  description?: string | null;
  /** Whether the scope is required. */
  required: boolean;
}
