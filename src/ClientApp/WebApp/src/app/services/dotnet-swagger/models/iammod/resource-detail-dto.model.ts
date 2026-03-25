/**
 * API resource detail DTO
 */
export interface ResourceDetailDto {
  /** id */
  id: string;
  /** name */
  name: string;
  /** displayName */
  displayName: string;
  /** description */
  description?: string | null;
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
