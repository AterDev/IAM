/**
 * Organization item DTO for list display
 */
export interface OrganizationItemDto {
  /** id */
  id: string;
  /** name */
  name: string;
  /** parentId */
  parentId?: string | null;
  /** level */
  level: number;
  /** displayOrder */
  displayOrder: number;
  /** description */
  description?: string | null;
  /** createdTime */
  createdTime: Date;
}
