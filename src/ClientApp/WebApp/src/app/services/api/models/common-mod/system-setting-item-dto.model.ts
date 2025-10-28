/**
 * System setting item DTO for list views
 */
export interface SystemSettingItemDto {
  /** id */
  id: string;
  /** key */
  key: string;
  /** value */
  value: string;
  /** category */
  category?: string | null;
  /** isEditable */
  isEditable: boolean;
  /** isPublic */
  isPublic: boolean;
}
