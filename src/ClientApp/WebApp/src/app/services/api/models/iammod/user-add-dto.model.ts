/**
 * User add DTO
 */
export interface UserAddDto {
  /** userName */
  userName: string;
  /** email */
  email: string;
  /** phoneNumber */
  phoneNumber?: string | null;
  /** password */
  password?: string | null;
  /** emailConfirmed */
  emailConfirmed: boolean;
  /** phoneNumberConfirmed */
  phoneNumberConfirmed: boolean;
  /** lockoutEnabled */
  lockoutEnabled: boolean;
}
