import { ClientType } from '../entity/client-type.model';
import { ConsentType } from '../entity/consent-type.model';
import { ApplicationType } from '../entity/application-type.model';

/**
 * Client update DTO
 */
export interface ClientUpdateDto {
  /** displayName */
  displayName?: string | null;
  /** description */
  description?: string | null;
  /** OAuth 2.0 client types */
  type?: ClientType | null;
  /** requirePkce */
  requirePkce?: boolean | null;
  /** OAuth 2.0 consent prompt types */
  consentType?: ConsentType | null;
  /** OAuth 2.0 application types */
  applicationType?: ApplicationType | null;
  /** redirectUris */
  redirectUris?: string[] | null;
  /** postLogoutRedirectUris */
  postLogoutRedirectUris?: string[] | null;
  /** scopeIds */
  scopeIds?: string[] | null;
  /** API resource IDs this client can access */
  resourceIds?: string[] | null;
}
