import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/perigon/page-list.model';
import { AuditLogItemDto } from '../models/iammod/audit-log-item-dto.model';
import { AuditLogDetailDto } from '../models/iammod/audit-log-detail-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class AuditTrailService extends BaseService {
  /**
   * getAuditLogs
   * @param category Filter by category
   * @param eventValue Filter by event
   * @param subjectId Filter by subject ID
   * @param startDate Filter by date range start
   * @param endDate Filter by date range end
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getAuditLogs(category: string | null, eventValue: string | null, subjectId: string | null, startDate: Date | null, endDate: Date | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<AuditLogItemDto>> {
    const _url = `/api/AuditTrail?Category=${category ?? ''}&Event=${eventValue ?? ''}&SubjectId=${subjectId ?? ''}&StartDate=${startDate ?? ''}&EndDate=${endDate ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<AuditLogItemDto>>('get', _url);
  }
  /**
   * getDetail
   * @param id string
   */
  getDetail(id: string): Observable<AuditLogDetailDto> {
    const _url = `/api/AuditTrail/${id}`;
    return this.request<AuditLogDetailDto>('get', _url);
  }
}