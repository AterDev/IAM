/**
 * Organization update DTO
 */
export interface OrganizationUpdateDto {
  /** name */
  name?: string | null;
  /** parentId */
  parentId?: string | null;
  /** displayOrder */
  displayOrder?: any | null;
  /** description */
  description?: string | null;
}
