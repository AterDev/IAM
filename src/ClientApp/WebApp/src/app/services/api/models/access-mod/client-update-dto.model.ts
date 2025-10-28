/**
 * Client update DTO
 */
export interface ClientUpdateDto {
  /** displayName */
  displayName?: string | null;
  /** description */
  description?: string | null;
  /** type */
  type?: string | null;
  /** requirePkce */
  requirePkce?: boolean | null;
  /** consentType */
  consentType?: string | null;
  /** applicationType */
  applicationType?: string | null;
  /** redirectUris */
  redirectUris?: string[] | null;
  /** postLogoutRedirectUris */
  postLogoutRedirectUris?: string[] | null;
}
