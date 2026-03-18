import { AuditLogDetailDto } from 'src/app/services/api/models/common-mod/audit-log-detail-dto.model';
import { AuditLogItemDto } from 'src/app/services/api/models/common-mod/audit-log-item-dto.model';

export type PasswordGrantEventFilter = 'all' | 'PasswordGrantRejected' | 'PasswordGrantFailed';

export interface PasswordGrantAuditSummary {
  total: number;
  rejected: number;
  failed: number;
}

export interface PasswordGrantAuditRow {
  log: AuditLogItemDto;
  detail: AuditLogDetailDto;
  clientId?: string | null;
  restrictionReason?: string | null;
  summary: string;
}
