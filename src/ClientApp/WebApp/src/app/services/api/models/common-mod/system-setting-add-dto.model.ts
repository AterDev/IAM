/**
 * System setting add DTO
 */
export interface SystemSettingAddDto {
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
}
