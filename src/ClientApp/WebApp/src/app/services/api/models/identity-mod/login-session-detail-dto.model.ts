/**
 * Login session detail DTO
 */
export interface LoginSessionDetailDto {
  /** id */
  id: string;
  /** userId */
  userId: string;
  /** sessionId */
  sessionId: string;
  /** ipAddress */
  ipAddress?: string | null;
  /** userAgent */
  userAgent?: string | null;
  /** deviceInfo */
  deviceInfo?: string | null;
  /** loginTime */
  loginTime: Date;
  /** lastActivityTime */
  lastActivityTime: Date;
  /** expirationTime */
  expirationTime?: Date | null;
  /** isActive */
  isActive: boolean;
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
