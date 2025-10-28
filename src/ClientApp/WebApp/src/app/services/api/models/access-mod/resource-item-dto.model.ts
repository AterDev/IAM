/**
 * API resource item DTO for list views
 */
export interface ResourceItemDto {
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
}
