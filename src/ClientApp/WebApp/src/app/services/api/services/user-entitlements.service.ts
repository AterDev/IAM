import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/perigon/page-list.model';
import { UserEntitlementDetailDto } from '../models/user-center-mod/user-entitlement-detail-dto.model';
import { UserEntitlementUpdateDto } from '../models/user-center-mod/user-entitlement-update-dto.model';
import { UserEntitlementAddDto } from '../models/user-center-mod/user-entitlement-add-dto.model';
/**
 * Administrator API for a user's entitlement assignments.
 */
@Injectable({ providedIn: 'root' })
export class UserEntitlementsService extends BaseService {
  /**
   * getPage
   * @param userId string
   * @param pageIndex number
   * @param pageSize number
   * @param orderBy Record<string, boolean>
   */
  getPage(userId: string | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<PageList<UserEntitlementDetailDto>> {
    const _url = `/api/UserEntitlements?userId=${userId ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<UserEntitlementDetailDto>>('get', _url);
  }
  /**
   * getDetail
   * @param id string
   */
  getDetail(id: string): Observable<UserEntitlementDetailDto> {
    const _url = `/api/UserEntitlements/${id}`;
    return this.request<UserEntitlementDetailDto>('get', _url);
  }
  /**
   * update
   * @param id string
   * @param data UserEntitlementUpdateDto
   */
  update(id: string, data: UserEntitlementUpdateDto): Observable<UserEntitlementDetailDto> {
    const _url = `/api/UserEntitlements/${id}`;
    return this.request<UserEntitlementDetailDto>('put', _url, data);
  }
  /**
   * delete
   * @param id string
   */
  delete(id: string): Observable<any> {
    const _url = `/api/UserEntitlements/${id}`;
    return this.request<any>('delete', _url);
  }
  /**
   * create
   * @param userId string
   * @param data UserEntitlementAddDto
   */
  create(userId: string, data: UserEntitlementAddDto): Observable<UserEntitlementDetailDto> {
    const _url = `/api/UserEntitlements/users/${userId}`;
    return this.request<UserEntitlementDetailDto>('post', _url, data);
  }
}