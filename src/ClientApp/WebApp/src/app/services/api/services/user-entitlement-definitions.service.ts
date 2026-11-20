import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/perigon/page-list.model';
import { UserEntitlementDefinitionItemDto } from '../models/user-center-mod/user-entitlement-definition-item-dto.model';
import { UserEntitlementDefinitionUpsertDto } from '../models/user-center-mod/user-entitlement-definition-upsert-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class UserEntitlementDefinitionsService extends BaseService {
  /**
   * getPage
   * @param keyword string
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getPage(keyword: string | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<UserEntitlementDefinitionItemDto>> {
    const _url = `/api/UserEntitlementDefinitions?Keyword=${keyword ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<UserEntitlementDefinitionItemDto>>('get', _url);
  }
  /**
   * create
   * @param data UserEntitlementDefinitionUpsertDto
   */
  create(data: UserEntitlementDefinitionUpsertDto): Observable<UserEntitlementDefinitionItemDto> {
    const _url = `/api/UserEntitlementDefinitions`;
    return this.request<UserEntitlementDefinitionItemDto>('post', _url, data);
  }
  /**
   * getDetail
   * @param id string
   */
  getDetail(id: string): Observable<UserEntitlementDefinitionItemDto> {
    const _url = `/api/UserEntitlementDefinitions/${id}`;
    return this.request<UserEntitlementDefinitionItemDto>('get', _url);
  }
  /**
   * update
   * @param id string
   * @param data UserEntitlementDefinitionUpsertDto
   */
  update(id: string, data: UserEntitlementDefinitionUpsertDto): Observable<UserEntitlementDefinitionItemDto> {
    const _url = `/api/UserEntitlementDefinitions/${id}`;
    return this.request<UserEntitlementDefinitionItemDto>('put', _url, data);
  }
  /**
   * delete
   * @param id string
   */
  delete(id: string): Observable<boolean> {
    const _url = `/api/UserEntitlementDefinitions/${id}`;
    return this.request<boolean>('delete', _url);
  }
}