import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/ater/page-list.model';
import { AuditLogItemDto } from '../models/common-mod/audit-log-item-dto.model';
import { AuditLogDetailDto } from '../models/common-mod/audit-log-detail-dto.model';
/**
 * Audit trail controller
 */
@Injectable({ providedIn: 'root' })
export class AuditTrailService extends BaseService {
  /**
   * Get paged audit logs
   * @param category Filter by category
   * @param event Filter by event
   * @param subjectId Filter by subject ID
   * @param startDate Filter by date range start
   * @param endDate Filter by date range end
   * @param pageIndex number
   * @param pageSize number
   * @param orderBy Record<string, boolean>
   */
  getAuditLogs(category: string | null, event: string | null, subjectId: string | null, startDate: Date | null, endDate: Date | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<PageList<AuditLogItemDto>> {
    const _url = `/api/AuditTrail?category=${category ?? ''}&event=${event ?? ''}&subjectId=${subjectId ?? ''}&startDate=${startDate ?? ''}&endDate=${endDate ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<AuditLogItemDto>>('get', _url);
  }
  /**
   * Get audit log detail by id
   * @param id Audit log id
   */
  getDetail(id: string): Observable<AuditLogDetailDto> {
    const _url = `/api/AuditTrail/${id}`;
    return this.request<AuditLogDetailDto>('get', _url);
  }
}