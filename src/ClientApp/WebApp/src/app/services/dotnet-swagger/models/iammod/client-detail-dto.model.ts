import { ClientType } from '../entity/client-type.model';
import { ConsentType } from '../entity/consent-type.model';
import { ApplicationType } from '../entity/application-type.model';
import { ClientRegistrationStatus } from '../entity/client-registration-status.model';
import { ScopeItemDto } from '../iammod/scope-item-dto.model';
import { ClientResourceDto } from '../iammod/client-resource-dto.model';
import { ClientSecretHistoryDto } from '../iammod/client-secret-history-dto.model';

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
  /** Lifecycle status for client self-service registration. */
  registrationStatus: ClientRegistrationStatus;
  /** developerUserId */
  developerUserId?: string | null;
  /** requestedTime */
  requestedTime?: Date | null;
  /** reviewedTime */
  reviewedTime?: Date | null;
  /** reviewedBy */
  reviewedBy?: string | null;
  /** secretExpiresAt */
  secretExpiresAt?: Date | null;
  /** allowPasswordGrant */
  allowPasswordGrant: boolean;
  /** passwordGrantRestrictionReason */
  passwordGrantRestrictionReason?: string | null;
  /** redirectUris */
  redirectUris: string[];
  /** postLogoutRedirectUris */
  postLogoutRedirectUris: string[];
  /** scopes */
  scopes: ScopeItemDto[];
  /** API resources this client can access */
  resources: ClientResourceDto[];
  /** secrets */
  secrets: ClientSecretHistoryDto[];
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
