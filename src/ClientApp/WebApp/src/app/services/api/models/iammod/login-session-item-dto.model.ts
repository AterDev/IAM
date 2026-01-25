/**
 * Login session item DTO for list views
 */
export interface LoginSessionItemDto {
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
  /** loginTime */
  loginTime: Date;
  /** lastActivityTime */
  lastActivityTime: Date;
  /** expirationTime */
  expirationTime?: Date | null;
  /** isActive */
  isActive: boolean;
}
