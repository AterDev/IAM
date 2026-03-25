import { ClientRegistrationStatus } from '../entity/client-registration-status.model';

/**
 * Result of a client self-service registration or approval action.
 */
export interface ClientRegistrationResultDto {
  /** id */
  id: string;
  /** clientId */
  clientId: string;
  /** Lifecycle status for client self-service registration. */
  registrationStatus: ClientRegistrationStatus;
  /** secret */
  secret?: string | null;
  /** message */
  message?: string | null;
}
