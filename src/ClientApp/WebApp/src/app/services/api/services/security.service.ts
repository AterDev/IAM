import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/perigon/page-list.model';
import { LoginSessionItemDto } from '../models/iammod/login-session-item-dto.model';
import { LoginSessionDetailDto } from '../models/iammod/login-session-detail-dto.model';
import { ApiMessageDto } from '../models/api-service/api-message-dto.model';
import { AuditLogFilterDto } from '../models/iammod/audit-log-filter-dto.model';
import { AuditLogItemDto } from '../models/iammod/audit-log-item-dto.model';
import { AuditLogDetailDto } from '../models/iammod/audit-log-detail-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class SecurityService extends BaseService {
  /**
   * getSessions
   * @param userId Filter by user ID
   * @param sessionId Filter by session ID
   * @param ipAddress Filter by IP address
   * @param isActive Filter by active status
   * @param startDate Filter by date range start
   * @param endDate Filter by date range end
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getSessions(userId: string | null, sessionId: string | null, ipAddress: string | null, isActive: boolean | null, startDate: Date | null, endDate: Date | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<LoginSessionItemDto>> {
    const _url = `/api/Security/sessions?UserId=${userId ?? ''}&SessionId=${sessionId ?? ''}&IpAddress=${ipAddress ?? ''}&IsActive=${isActive ?? ''}&StartDate=${startDate ?? ''}&EndDate=${endDate ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<LoginSessionItemDto>>('get', _url);
  }
  /**
   * getSessionDetail
   * @param id string
   */
  getSessionDetail(id: string): Observable<LoginSessionDetailDto> {
    const _url = `/api/Security/sessions/${id}`;
    return this.request<LoginSessionDetailDto>('get', _url);
  }
  /**
   * revokeSession
   * @param id string
   */
  revokeSession(id: string): Observable<ApiMessageDto> {
    const _url = `/api/Security/sessions/${id}/revoke`;
    return this.request<ApiMessageDto>('post', _url);
  }
  /**
   * revokeAllSessions
   * @param exceptCurrent boolean
   */
  revokeAllSessions(exceptCurrent: boolean): Observable<ApiMessageDto> {
    const _url = `/api/Security/sessions/revoke-all?exceptCurrent=${exceptCurrent ?? ''}`;
    return this.request<ApiMessageDto>('post', _url);
  }
  /**
   * getAuditLogs
   * @param data AuditLogFilterDto
   */
  getAuditLogs(data: AuditLogFilterDto): Observable<PageList<AuditLogItemDto>> {
    const _url = `/api/Security/logs`;
    return this.request<PageList<AuditLogItemDto>>('post', _url, data);
  }
  /**
   * getAuditLogDetail
   * @param id string
   */
  getAuditLogDetail(id: string): Observable<AuditLogDetailDto> {
    const _url = `/api/Security/logs/${id}`;
    return this.request<AuditLogDetailDto>('get', _url);
  }
}