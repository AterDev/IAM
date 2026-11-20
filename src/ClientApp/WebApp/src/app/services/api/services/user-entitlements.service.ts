import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/perigon/page-list.model';
import { UserEntitlementDetailDto } from '../models/user-center-mod/user-entitlement-detail-dto.model';
import { UserEntitlementUpdateDto } from '../models/user-center-mod/user-entitlement-update-dto.model';
import { UserEntitlementAddDto } from '../models/user-center-mod/user-entitlement-add-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class UserEntitlementsService extends BaseService {
  /**
   * getPage
   * @param userId string
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getPage(userId: string, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<UserEntitlementDetailDto>> {
    const _url = `/api/UserEntitlements?UserId=${userId ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
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
  delete(id: string): Observable<boolean> {
    const _url = `/api/UserEntitlements/${id}`;
    return this.request<boolean>('delete', _url);
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