/**
 * Self-service password change payload for the current authenticated user.
 */
export interface ChangePasswordRequestDto {
  /** currentPassword */
  currentPassword: string;
  /** newPassword */
  newPassword: string;
}
