/**
 * Client update DTO
 */
export interface ClientUpdateDto {
  /** displayName */
  displayName?: string | null;
  /** description */
  description?: string | null;
  /** type */
  type?: number | null;
  /** requirePkce */
  requirePkce?: boolean | null;
  /** consentType */
  consentType?: number | null;
  /** applicationType */
  applicationType?: number | null;
  /** redirectUris */
  redirectUris?: string[] | null;
  /** postLogoutRedirectUris */
  postLogoutRedirectUris?: string[] | null;
  /** scopeIds */
  scopeIds?: string[] | null;
  /** API resource IDs this client can access */
  resourceIds?: string[] | null;
}
