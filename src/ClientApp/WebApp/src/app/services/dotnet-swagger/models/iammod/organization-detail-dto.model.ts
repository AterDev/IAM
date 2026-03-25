/**
 * Organization detail DTO
 */
export interface OrganizationDetailDto {
  /** id */
  id: string;
  /** name */
  name: string;
  /** parentId */
  parentId?: string | null;
  /** path */
  path?: string | null;
  /** level */
  level: number;
  /** displayOrder */
  displayOrder: number;
  /** description */
  description?: string | null;
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
