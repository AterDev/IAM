/**
 * Organization add DTO
 */
export interface OrganizationAddDto {
  /** name */
  name: string;
  /** parentId */
  parentId?: string | null;
  /** displayOrder */
  displayOrder: any;
  /** description */
  description?: string | null;
}
