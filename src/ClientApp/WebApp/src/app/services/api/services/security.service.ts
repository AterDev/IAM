import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/ater/page-list.model';
import { LoginSessionItemDto } from '../models/identity-mod/login-session-item-dto.model';
import { LoginSessionDetailDto } from '../models/identity-mod/login-session-detail-dto.model';
import { AuditLogItemDto } from '../models/common-mod/audit-log-item-dto.model';
import { AuditLogDetailDto } from '../models/common-mod/audit-log-detail-dto.model';
/**
 * Security controller for session and audit log management
 */
@Injectable({ providedIn: 'root' })
export class SecurityService extends BaseService {
  /**
   * Get paged login sessions
   * @param userId Filter by user ID
   * @param sessionId Filter by session ID
   * @param ipAddress Filter by IP address
   * @param isActive Filter by active status
   * @param startDate Filter by date range start
   * @param endDate Filter by date range end
   * @param pageIndex number
   * @param pageSize number
   * @param orderBy Record<string, boolean>
   */
  getSessions(userId: string | null, sessionId: string | null, ipAddress: string | null, isActive: boolean | null, startDate: Date | null, endDate: Date | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<PageList<LoginSessionItemDto>> {
    const _url = `/api/security/sessions?userId=${userId ?? ''}&sessionId=${sessionId ?? ''}&ipAddress=${ipAddress ?? ''}&isActive=${isActive ?? ''}&startDate=${startDate ?? ''}&endDate=${endDate ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<LoginSessionItemDto>>('get', _url);
  }
  /**
   * Get login session detail by id
   * @param id Login session id
   */
  getSessionDetail(id: string): Observable<LoginSessionDetailDto> {
    const _url = `/api/security/sessions/${id}`;
    return this.request<LoginSessionDetailDto>('get', _url);
  }
  /**
   * Revoke a login session
   * @param id Login session id
   */
  revokeSession(id: string): Observable<any> {
    const _url = `/api/security/sessions/${id}/revoke`;
    return this.request<any>('post', _url);
  }
  /**
   * Revoke all sessions for the current user
   * @param exceptCurrent Whether to keep the current session active
   */
  revokeAllSessions(exceptCurrent: boolean | null): Observable<any> {
    const _url = `/api/security/sessions/revoke-all?exceptCurrent=${exceptCurrent ?? ''}`;
    return this.request<any>('post', _url);
  }
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
    const _url = `/api/security/logs?category=${category ?? ''}&event=${event ?? ''}&subjectId=${subjectId ?? ''}&startDate=${startDate ?? ''}&endDate=${endDate ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<AuditLogItemDto>>('get', _url);
  }
  /**
   * Get audit log detail by id
   * @param id Audit log id
   */
  getAuditLogDetail(id: string): Observable<AuditLogDetailDto> {
    const _url = `/api/security/logs/${id}`;
    return this.request<AuditLogDetailDto>('get', _url);
  }
}