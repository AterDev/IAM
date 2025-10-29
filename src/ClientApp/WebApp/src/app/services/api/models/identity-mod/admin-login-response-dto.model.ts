import { AdminUserInfo } from '../identity-mod/admin-user-info.model';

/**
 * Admin login response DTO
 */
export interface AdminLoginResponseDto {
  /** JWT access token */
  accessToken: string;
  /** Token type (Bearer) */
  tokenType: string;
  /** Token expiration in seconds */
  expiresIn: number;
  /** Admin user information */
  user: AdminUserInfo;
}
