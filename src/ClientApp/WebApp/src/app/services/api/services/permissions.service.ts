import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/perigon/page-list.model';
import { PermissionType } from '../models/entity/permission-type.model';
import { PermissionItemDto } from '../models/iammod/permission-item-dto.model';
import { PermissionUpsertDto } from '../models/iammod/permission-upsert-dto.model';
import { PermissionDetailDto } from '../models/iammod/permission-detail-dto.model';
import { PermissionTreeNodeDto } from '../models/iammod/permission-tree-node-dto.model';
import { UserPermissionDto } from '../models/iammod/user-permission-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class PermissionsService extends BaseService {
  /**
   * getPermissions
   * @param clientId Filter by database client id.
   * @param clientCode Filter by public client identifier.
   * @param type Filter by permission type.
   * @param parentId Filter by parent permission id.
   * @param keyword Filter by keyword.
   * @param onlyNonBusiness Whether to include only menu and button permissions.
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getPermissions(clientId: string | null, clientCode: string | null, type: PermissionType | null, parentId: string | null, keyword: string | null, onlyNonBusiness: boolean | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<PermissionItemDto>> {
    const _url = `/api/Permissions?ClientId=${clientId ?? ''}&ClientCode=${clientCode ?? ''}&Type=${type ?? ''}&ParentId=${parentId ?? ''}&Keyword=${keyword ?? ''}&OnlyNonBusiness=${onlyNonBusiness ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<PermissionItemDto>>('get', _url);
  }
  /**
   * create
   * @param data PermissionUpsertDto
   */
  create(data: PermissionUpsertDto): Observable<PermissionDetailDto> {
    const _url = `/api/Permissions`;
    return this.request<PermissionDetailDto>('post', _url, data);
  }
  /**
   * getPermissionTree
   * @param clientId Filter by database client id.
   * @param clientCode Filter by public client identifier.
   * @param type Filter by permission type.
   * @param parentId Filter by parent permission id.
   * @param keyword Filter by keyword.
   * @param onlyNonBusiness Whether to include only menu and button permissions.
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getPermissionTree(clientId: string | null, clientCode: string | null, type: PermissionType | null, parentId: string | null, keyword: string | null, onlyNonBusiness: boolean | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PermissionTreeNodeDto[]> {
    const _url = `/api/Permissions/tree?ClientId=${clientId ?? ''}&ClientCode=${clientCode ?? ''}&Type=${type ?? ''}&ParentId=${parentId ?? ''}&Keyword=${keyword ?? ''}&OnlyNonBusiness=${onlyNonBusiness ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PermissionTreeNodeDto[]>('get', _url);
  }
  /**
   * getUserPermissions
   */
  getUserPermissions(): Observable<UserPermissionDto[]> {
    const _url = `/api/Permissions/user-permissions`;
    return this.request<UserPermissionDto[]>('get', _url);
  }
  /**
   * getDetail
   * @param id string
   */
  getDetail(id: string): Observable<PermissionDetailDto> {
    const _url = `/api/Permissions/${id}`;
    return this.request<PermissionDetailDto>('get', _url);
  }
  /**
   * update
   * @param id string
   * @param data PermissionUpsertDto
   */
  update(id: string, data: PermissionUpsertDto): Observable<PermissionDetailDto> {
    const _url = `/api/Permissions/${id}`;
    return this.request<PermissionDetailDto>('put', _url, data);
  }
  /**
   * delete
   * @param id string
   */
  delete(id: string): Observable<boolean> {
    const _url = `/api/Permissions/${id}`;
    return this.request<boolean>('delete', _url);
  }
}