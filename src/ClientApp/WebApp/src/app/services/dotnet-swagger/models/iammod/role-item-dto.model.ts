/**
 * Role item DTO for list display
 */
export interface RoleItemDto {
  /** id */
  id: string;
  /** name */
  name: string;
  /** description */
  description?: string | null;
  /** createdTime */
  createdTime: Date;
}
