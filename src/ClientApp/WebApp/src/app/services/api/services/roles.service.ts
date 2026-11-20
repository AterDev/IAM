import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/perigon/page-list.model';
import { RoleItemDto } from '../models/iammod/role-item-dto.model';
import { RoleAddDto } from '../models/iammod/role-add-dto.model';
import { RoleDetailDto } from '../models/iammod/role-detail-dto.model';
import { RoleUpdateDto } from '../models/iammod/role-update-dto.model';
import { RoleGrantPermissionDto } from '../models/iammod/role-grant-permission-dto.model';
import { PermissionTreeNodeDto } from '../models/iammod/permission-tree-node-dto.model';
import { PermissionType } from '../models/entity/permission-type.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class RolesService extends BaseService {
  /**
   * getRoles
   * @param name Filter by role name
   * @param startDate Filter by date range start
   * @param endDate Filter by date range end
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getRoles(name: string | null, startDate: Date | null, endDate: Date | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<RoleItemDto>> {
    const _url = `/api/Roles?Name=${name ?? ''}&StartDate=${startDate ?? ''}&EndDate=${endDate ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<RoleItemDto>>('get', _url);
  }
  /**
   * createRole
   * @param data RoleAddDto
   */
  createRole(data: RoleAddDto): Observable<RoleDetailDto> {
    const _url = `/api/Roles`;
    return this.request<RoleDetailDto>('post', _url, data);
  }
  /**
   * getAllRoles
   */
  getAllRoles(): Observable<RoleItemDto[]> {
    const _url = `/api/Roles/all`;
    return this.request<RoleItemDto[]>('get', _url);
  }
  /**
   * getByName
   * @param name string
   */
  getByName(name: string): Observable<RoleDetailDto> {
    const _url = `/api/Roles/name/${name}`;
    return this.request<RoleDetailDto>('get', _url);
  }
  /**
   * getDetail
   * @param id string
   */
  getDetail(id: string): Observable<RoleDetailDto> {
    const _url = `/api/Roles/${id}`;
    return this.request<RoleDetailDto>('get', _url);
  }
  /**
   * updateRole
   * @param id string
   * @param data RoleUpdateDto
   */
  updateRole(id: string, data: RoleUpdateDto): Observable<RoleDetailDto> {
    const _url = `/api/Roles/${id}`;
    return this.request<RoleDetailDto>('put', _url, data);
  }
  /**
   * deleteRole
   * @param id string
   * @param hardDelete boolean
   */
  deleteRole(id: string, hardDelete: boolean): Observable<boolean> {
    const _url = `/api/Roles/${id}?hardDelete=${hardDelete ?? ''}`;
    return this.request<boolean>('delete', _url);
  }
  /**
   * grantPermissions
   * @param id string
   * @param data RoleGrantPermissionDto
   */
  grantPermissions(id: string, data: RoleGrantPermissionDto): Observable<boolean> {
    const _url = `/api/Roles/${id}/permissions`;
    return this.request<boolean>('post', _url, data);
  }
  /**
   * getPermissions
   * @param id string
   */
  getPermissions(id: string): Observable<string[]> {
    const _url = `/api/Roles/${id}/permissions`;
    return this.request<string[]>('get', _url);
  }
  /**
   * getPermissionTree
   * @param id string
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
  getPermissionTree(id: string, clientId: string | null, clientCode: string | null, type: PermissionType | null, parentId: string | null, keyword: string | null, onlyNonBusiness: boolean | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PermissionTreeNodeDto[]> {
    const _url = `/api/Roles/${id}/permission-tree?ClientId=${clientId ?? ''}&ClientCode=${clientCode ?? ''}&Type=${type ?? ''}&ParentId=${parentId ?? ''}&Keyword=${keyword ?? ''}&OnlyNonBusiness=${onlyNonBusiness ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PermissionTreeNodeDto[]>('get', _url);
  }
}