/**
 * Client resource DTO
 */
export interface ClientResourceDto {
  /** Resource ID */
  id: string;
  /** Resource name */
  name: string;
  /** Resource display name */
  displayName: string;
  /** Resource description */
  description?: string | null;
  /** Creation time */
  createdTime: Date;
}
