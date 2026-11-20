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
  type?: number | null;
  /** requirePkce */
  requirePkce: boolean;
  /** consentType */
  consentType?: number | null;
  /** applicationType */
  applicationType?: number | null;
  /** redirectUris */
  redirectUris: string[];
  /** postLogoutRedirectUris */
  postLogoutRedirectUris: string[];
  /** scopeIds */
  scopeIds: string[];
  /** API resource IDs this client can access */
  resourceIds: string[];
}
