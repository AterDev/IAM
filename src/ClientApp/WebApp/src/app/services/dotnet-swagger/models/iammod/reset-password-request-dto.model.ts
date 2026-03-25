/**
 * Password reset confirmation payload.
 */
export interface ResetPasswordRequestDto {
  /** email */
  email: string;
  /** code */
  code: string;
  /** newPassword */
  newPassword: string;
}
