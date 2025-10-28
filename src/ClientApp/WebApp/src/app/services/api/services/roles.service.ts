import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/ater/page-list.model';
import { RoleItemDto } from '../models/identity-mod/role-item-dto.model';
import { RoleAddDto } from '../models/identity-mod/role-add-dto.model';
import { RoleDetailDto } from '../models/identity-mod/role-detail-dto.model';
import { RoleUpdateDto } from '../models/identity-mod/role-update-dto.model';
import { RoleGrantPermissionDto } from '../models/identity-mod/role-grant-permission-dto.model';
import { PermissionClaim } from '../models/identity-mod/permission-claim.model';
/**
 * Role management controller
 */
@Injectable({ providedIn: 'root' })
export class RolesService extends BaseService {
  /**
   * Get paged roles
   * @param name Filter by role name
   * @param startDate Filter by date range start
   * @param endDate Filter by date range end
   * @param pageIndex number
   * @param pageSize number
   * @param orderBy Record<string, boolean>
   */
  getRoles(name: string | null, startDate: Date | null, endDate: Date | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<PageList<RoleItemDto>> {
    const _url = `/api/Roles?name=${name ?? ''}&startDate=${startDate ?? ''}&endDate=${endDate ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<RoleItemDto>>('get', _url);
  }
  /**
   * Create new role
   * @param data RoleAddDto
   */
  createRole(data: RoleAddDto): Observable<RoleDetailDto> {
    const _url = `/api/Roles`;
    return this.request<RoleDetailDto>('post', _url, data);
  }
  /**
   * Get all roles
   */
  getAllRoles(): Observable<RoleItemDto[]> {
    const _url = `/api/Roles/all`;
    return this.request<RoleItemDto[]>('get', _url);
  }
  /**
   * Get role detail by id
   * @param id Role id
   */
  getDetail(id: string): Observable<RoleDetailDto> {
    const _url = `/api/Roles/${id}`;
    return this.request<RoleDetailDto>('get', _url);
  }
  /**
   * Update role
   * @param id Role id
   * @param data RoleUpdateDto
   */
  updateRole(id: string, data: RoleUpdateDto): Observable<RoleDetailDto> {
    const _url = `/api/Roles/${id}`;
    return this.request<RoleDetailDto>('put', _url, data);
  }
  /**
   * Delete role
   * @param id Role id
   * @param hardDelete Perform hard delete (default false)
   */
  deleteRole(id: string, hardDelete: boolean | null): Observable<any> {
    const _url = `/api/Roles/${id}?hardDelete=${hardDelete ?? ''}`;
    return this.request<any>('delete', _url);
  }
  /**
   * Get role by name
   * @param name Role name
   */
  getByName(name: string): Observable<RoleDetailDto> {
    const _url = `/api/Roles/name/${name}`;
    return this.request<RoleDetailDto>('get', _url);
  }
  /**
   * Grant permissions to role
   * @param id Role id
   * @param data RoleGrantPermissionDto
   */
  grantPermissions(id: string, data: RoleGrantPermissionDto): Observable<any> {
    const _url = `/api/Roles/${id}/permissions`;
    return this.request<any>('post', _url, data);
  }
  /**
   * Get role permissions
   * @param id Role id
   */
  getPermissions(id: string): Observable<PermissionClaim[]> {
    const _url = `/api/Roles/${id}/permissions`;
    return this.request<PermissionClaim[]>('get', _url);
  }
}