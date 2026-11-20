/**
 * Organization tree DTO for hierarchical display
 */
export interface OrganizationTreeDto {
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
  /** children */
  children: OrganizationTreeDto[];
}
