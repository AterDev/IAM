/**
 * Admin user information
 */
export interface AdminUserInfo {
  /** User ID */
  id: string;
  /** Username */
  userName: string;
  /** Email address */
  email?: string | null;
  /** User roles */
  roles: string[];
}
