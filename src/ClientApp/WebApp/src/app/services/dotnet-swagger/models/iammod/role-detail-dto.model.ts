/**
 * Role detail DTO
 */
export interface RoleDetailDto {
  /** id */
  id: string;
  /** name */
  name: string;
  /** description */
  description?: string | null;
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
