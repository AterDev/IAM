import { AdminUserInfo } from '../iammod/admin-user-info.model';

/**
 * Admin login response DTO
 */
export interface AdminLoginResponseDto {
  /** JWT access token */
  accessToken: string;
  /** Token type (Bearer) */
  tokenType: string;
  /** Token expiration in seconds */
  expiresIn: any;
  /** Unified session identifier */
  sessionId: string;
  /** User information */
  user: AdminUserInfo;
}
