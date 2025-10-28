/**
 * System setting detail DTO
 */
export interface SystemSettingDetailDto {
  /** id */
  id: string;
  /** key */
  key: string;
  /** value */
  value: string;
  /** description */
  description?: string | null;
  /** category */
  category?: string | null;
  /** isEditable */
  isEditable: boolean;
  /** isPublic */
  isPublic: boolean;
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
