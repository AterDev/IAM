import { ClientType } from '../entity/client-type.model';
import { ConsentType } from '../entity/consent-type.model';
import { ApplicationType } from '../entity/application-type.model';

/**
 * Self-service client registration request.
 */
export interface ClientRegistrationRequestDto {
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
  /** allowPasswordGrant */
  allowPasswordGrant: boolean;
  /** passwordGrantRestrictionReason */
  passwordGrantRestrictionReason?: string | null;
  /** redirectUris */
  redirectUris: string[];
  /** postLogoutRedirectUris */
  postLogoutRedirectUris: string[];
  /** scopeIds */
  scopeIds: string[];
  /** resourceIds */
  resourceIds: string[];
}
