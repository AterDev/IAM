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
  level: any;
  /** displayOrder */
  displayOrder: any;
  /** description */
  description?: string | null;
  /** createdTime */
  createdTime: Date;
}
