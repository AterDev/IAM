/**
 * Device authorization request DTO
 */
export interface DeviceAuthorizationRequestDto {
  /** Client identifier */
  clientId: string;
  /** Requested scope */
  scope?: string | null;
}
