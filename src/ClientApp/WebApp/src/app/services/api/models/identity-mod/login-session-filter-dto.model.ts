/**
 * Login session filter DTO
 */
export interface LoginSessionFilterDto {
  /** userId */
  userId?: string | null;
  /** sessionId */
  sessionId?: string | null;
  /** ipAddress */
  ipAddress?: string | null;
  /** isActive */
  isActive?: boolean | null;
  /** startDate */
  startDate?: Date | null;
  /** endDate */
  endDate?: Date | null;
  /** pageIndex */
  pageIndex?: number | null;
  /** pageSize */
  pageSize?: number | null;
  /** orderBy */
  orderBy?: Record<string, boolean> | null;
}
