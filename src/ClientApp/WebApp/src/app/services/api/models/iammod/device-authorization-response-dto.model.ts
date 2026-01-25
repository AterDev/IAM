/**
 * Device authorization response DTO
 */
export interface DeviceAuthorizationResponseDto {
  /** Device code */
  deviceCode: string;
  /** User code */
  userCode: string;
  /** Verification URI */
  verificationUri: string;
  /** Verification URI complete (optional) */
  verificationUriComplete?: string | null;
  /** Expires in seconds */
  expiresIn: number;
  /** Interval for polling in seconds */
  interval: number;
}
