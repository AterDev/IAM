/**
 * Audit log filter DTO
 */
export interface AuditLogFilterDto {
  /** pageIndex */
  pageIndex?: number | null;
  /** pageSize */
  pageSize?: number | null;
  /** orderBy */
  orderBy?: Record<string, boolean> | null;
  /** Filter by category */
  category?: string | null;
  /** Filter by event */
  event?: string | null;
  /** Filter by subject ID */
  subjectId?: string | null;
  /** Filter by date range start */
  startDate?: Date | null;
  /** Filter by date range end */
  endDate?: Date | null;
}
