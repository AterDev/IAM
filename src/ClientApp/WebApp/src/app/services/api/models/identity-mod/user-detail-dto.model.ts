/**
 * User detail DTO
 */
export interface UserDetailDto {
  /** id */
  id: string;
  /** userName */
  userName: string;
  /** email */
  email?: string | null;
  /** emailConfirmed */
  emailConfirmed: boolean;
  /** phoneNumber */
  phoneNumber?: string | null;
  /** phoneNumberConfirmed */
  phoneNumberConfirmed: boolean;
  /** isTwoFactorEnabled */
  isTwoFactorEnabled: boolean;
  /** lockoutEnd */
  lockoutEnd?: Date | null;
  /** lockoutEnabled */
  lockoutEnabled: boolean;
  /** accessFailedCount */
  accessFailedCount: number;
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
