/**
 * Audit log detail DTO
 */
export interface AuditLogDetailDto {
  /** id */
  id: string;
  /** category */
  category: string;
  /** event */
  event: string;
  /** subjectId */
  subjectId?: string | null;
  /** payload */
  payload?: string | null;
  /** ipAddress */
  ipAddress?: string | null;
  /** userAgent */
  userAgent?: string | null;
  /** createdTime */
  createdTime: Date;
}
