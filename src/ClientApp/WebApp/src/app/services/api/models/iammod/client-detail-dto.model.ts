import { ClientType } from '../entity/client-type.model';
import { ConsentType } from '../entity/consent-type.model';
import { ApplicationType } from '../entity/application-type.model';
import { ScopeItemDto } from '../iammod/scope-item-dto.model';
import { ClientResourceDto } from '../iammod/client-resource-dto.model';

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
  /** OAuth 2.0 client types */
  type: ClientType;
  /** requirePkce */
  requirePkce: boolean;
  /** OAuth 2.0 consent prompt types */
  consentType: ConsentType;
  /** OAuth 2.0 application types */
  applicationType: ApplicationType;
  /** redirectUris */
  redirectUris: string[];
  /** postLogoutRedirectUris */
  postLogoutRedirectUris: string[];
  /** scopes */
  scopes: ScopeItemDto[];
  /** API resources this client can access */
  resources: ClientResourceDto[];
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
