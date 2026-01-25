import { ClientType } from '../entity/client-type.model';
import { ApplicationType } from '../entity/application-type.model';

/**
 * Client item DTO for list display
 */
export interface ClientItemDto {
  /** id */
  id: string;
  /** clientId */
  clientId: string;
  /** displayName */
  displayName: string;
  /** OAuth 2.0 client types */
  type: ClientType;
  /** OAuth 2.0 application types */
  applicationType: ApplicationType;
  /** createdTime */
  createdTime: Date;
}
