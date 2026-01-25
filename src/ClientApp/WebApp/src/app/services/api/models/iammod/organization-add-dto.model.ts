/**
 * Organization add DTO
 */
export interface OrganizationAddDto {
  /** name */
  name: string;
  /** parentId */
  parentId?: string | null;
  /** displayOrder */
  displayOrder: number;
  /** description */
  description?: string | null;
}
