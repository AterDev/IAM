/**
 * Client add DTO
 */
export interface ClientAddDto {
  /** clientId */
  clientId: string;
  /** displayName */
  displayName: string;
  /** description */
  description?: string | null;
  /** type */
  type?: string | null;
  /** requirePkce */
  requirePkce: boolean;
  /** consentType */
  consentType?: string | null;
  /** applicationType */
  applicationType?: string | null;
  /** redirectUris */
  redirectUris: string[];
  /** postLogoutRedirectUris */
  postLogoutRedirectUris: string[];
  /** scopeIds */
  scopeIds: string[];
}
