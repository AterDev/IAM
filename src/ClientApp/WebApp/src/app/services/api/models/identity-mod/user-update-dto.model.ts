/**
 * User update DTO
 */
export interface UserUpdateDto {
  /** email */
  email?: string | null;
  /** phoneNumber */
  phoneNumber?: string | null;
  /** emailConfirmed */
  emailConfirmed?: boolean | null;
  /** phoneNumberConfirmed */
  phoneNumberConfirmed?: boolean | null;
  /** isTwoFactorEnabled */
  isTwoFactorEnabled?: boolean | null;
  /** lockoutEnabled */
  lockoutEnabled?: boolean | null;
}
