/**
 * Audit log item DTO for list views
 */
export interface AuditLogItemDto {
  /** id */
  id: string;
  /** category */
  category: string;
  /** event */
  event: string;
  /** subjectId */
  subjectId?: string | null;
  /** ipAddress */
  ipAddress?: string | null;
  /** createdTime */
  createdTime: Date;
}
