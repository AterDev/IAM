/**
 * User item DTO for list display
 */
export interface UserItemDto {
  /** id */
  id: string;
  /** userName */
  userName: string;
  /** email */
  email?: string | null;
  /** phoneNumber */
  phoneNumber?: string | null;
  /** lockoutEnabled */
  lockoutEnabled: boolean;
  /** createdTime */
  createdTime: Date;
}
