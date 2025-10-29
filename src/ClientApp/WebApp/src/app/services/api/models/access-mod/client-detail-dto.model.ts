/**
 * Client detail DTO
 */
export interface ClientDetailDto {
  /** id */
  id: string;
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
  /** scopes */
  scopes: string[];
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
