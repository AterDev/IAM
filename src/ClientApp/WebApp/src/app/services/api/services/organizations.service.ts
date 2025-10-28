import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/ater/page-list.model';
import { OrganizationItemDto } from '../models/identity-mod/organization-item-dto.model';
import { OrganizationAddDto } from '../models/identity-mod/organization-add-dto.model';
import { OrganizationDetailDto } from '../models/identity-mod/organization-detail-dto.model';
import { OrganizationTreeDto } from '../models/identity-mod/organization-tree-dto.model';
import { OrganizationUpdateDto } from '../models/identity-mod/organization-update-dto.model';
/**
 * Organization management controller
 */
@Injectable({ providedIn: 'root' })
export class OrganizationsService extends BaseService {
  /**
   * Get paged organizations
   * @param name Filter by organization name
   * @param parentId Filter by parent ID
   * @param level Filter by level
   * @param pageIndex number
   * @param pageSize number
   * @param orderBy Record<string, boolean>
   */
  getOrganizations(name: string | null, parentId: string | null, level: number | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<PageList<OrganizationItemDto>> {
    const _url = `/api/Organizations?name=${name ?? ''}&parentId=${parentId ?? ''}&level=${level ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<OrganizationItemDto>>('get', _url);
  }
  /**
   * Create new organization
   * @param data OrganizationAddDto
   */
  createOrganization(data: OrganizationAddDto): Observable<OrganizationDetailDto> {
    const _url = `/api/Organizations`;
    return this.request<OrganizationDetailDto>('post', _url, data);
  }
  /**
   * Get organization tree
   * @param parentId Parent organization id (null for root)
   */
  getTree(parentId: string | null): Observable<OrganizationTreeDto[]> {
    const _url = `/api/Organizations/tree?parentId=${parentId ?? ''}`;
    return this.request<OrganizationTreeDto[]>('get', _url);
  }
  /**
   * Get organization detail by id
   * @param id Organization id
   */
  getDetail(id: string): Observable<OrganizationDetailDto> {
    const _url = `/api/Organizations/${id}`;
    return this.request<OrganizationDetailDto>('get', _url);
  }
  /**
   * Update organization
   * @param id Organization id
   * @param data OrganizationUpdateDto
   */
  updateOrganization(id: string, data: OrganizationUpdateDto): Observable<OrganizationDetailDto> {
    const _url = `/api/Organizations/${id}`;
    return this.request<OrganizationDetailDto>('put', _url, data);
  }
  /**
   * Delete organization
   * @param id Organization id
   * @param hardDelete Perform hard delete (default false)
   */
  deleteOrganization(id: string, hardDelete: boolean | null): Observable<any> {
    const _url = `/api/Organizations/${id}?hardDelete=${hardDelete ?? ''}`;
    return this.request<any>('delete', _url);
  }
  /**
   * Add users to organization
   * @param id Organization id
   * @param data string[]
   */
  addUsers(id: string, data: string[]): Observable<any> {
    const _url = `/api/Organizations/${id}/users`;
    return this.request<any>('post', _url, data);
  }
  /**
   * Remove users from organization
   * @param id Organization id
   * @param data string[]
   */
  removeUsers(id: string, data: string[]): Observable<any> {
    const _url = `/api/Organizations/${id}/users`;
    return this.request<any>('delete', _url, data);
  }
}