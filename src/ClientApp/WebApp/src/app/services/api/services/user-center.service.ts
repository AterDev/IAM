import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserCenterLoginDto } from '../models/user-center-mod/user-center-login-dto.model';
import { ProxyUsageAddDto } from '../models/user-center-mod/proxy-usage-add-dto.model';
import { PageList } from '../models/perigon/page-list.model';
import { ProxyUsageItemDto } from '../models/user-center-mod/proxy-usage-item-dto.model';
import { UserEntitlementDetailDto } from '../models/user-center-mod/user-entitlement-detail-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class UserCenterService extends BaseService {
  /**
   * login
   * @param data UserCenterLoginDto
   */
  login(data: UserCenterLoginDto): Observable<string> {
    const _url = `/api/UserCenter/login`;
    return this.request<string>('post', _url, data);
  }
  /**
   * addProxyUsage
   * @param data ProxyUsageAddDto
   */
  addProxyUsage(data: ProxyUsageAddDto): Observable<any> {
    const _url = `/api/UserCenter/proxyUsage`;
    return this.request<any>('post', _url, data);
  }
  /**
   * getProxyUsage
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getProxyUsage(pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<ProxyUsageItemDto>> {
    const _url = `/api/UserCenter/proxyUsage?pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<ProxyUsageItemDto>>('get', _url);
  }
  /**
   * getEntitlement
   * @param entitlementCode string
   */
  getEntitlement(entitlementCode: string): Observable<UserEntitlementDetailDto> {
    const _url = `/api/UserCenter/GetEntitlement?entitlementCode=${entitlementCode ?? ''}`;
    return this.request<UserEntitlementDetailDto>('get', _url);
  }
}