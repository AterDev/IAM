import { ScopeItemDto } from './scope-item-dto.model';
import { ResourceItemDto } from './resource-item-dto.model';

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
  scopes: ScopeItemDto[];
  /** resources - API resources this client can access */
  resources: ResourceItemDto[];
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
