/**
 * User authorization DTO
 */
export interface UserAuthorizationDto {
  /** id */
  id: string;
  /** clientId */
  clientId: string;
  /** clientName */
  clientName: string;
  /** scopes */
  scopes: string;
  /** type */
  type: string;
  /** status */
  status: string;
  /** creationDate */
  creationDate: Date;
  /** expirationDate */
  expirationDate?: Date | null;
}
