import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/perigon/page-list.model';
import { OrganizationItemDto } from '../models/iammod/organization-item-dto.model';
import { OrganizationAddDto } from '../models/iammod/organization-add-dto.model';
import { OrganizationDetailDto } from '../models/iammod/organization-detail-dto.model';
import { OrganizationTreeDto } from '../models/iammod/organization-tree-dto.model';
import { OrganizationUpdateDto } from '../models/iammod/organization-update-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class OrganizationsService extends BaseService {
  /**
   * getOrganizations
   * @param name Filter by organization name
   * @param parentId Filter by parent ID
   * @param level Filter by level
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getOrganizations(name: string | null, parentId: string | null, level: any | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<OrganizationItemDto>> {
    const _url = `/api/Organizations?Name=${name ?? ''}&ParentId=${parentId ?? ''}&Level=${level ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<OrganizationItemDto>>('get', _url);
  }
  /**
   * createOrganization
   * @param data OrganizationAddDto
   */
  createOrganization(data: OrganizationAddDto): Observable<OrganizationDetailDto> {
    const _url = `/api/Organizations`;
    return this.request<OrganizationDetailDto>('post', _url, data);
  }
  /**
   * getTree
   * @param parentId string
   */
  getTree(parentId: string | null): Observable<OrganizationTreeDto[]> {
    const _url = `/api/Organizations/tree?parentId=${parentId ?? ''}`;
    return this.request<OrganizationTreeDto[]>('get', _url);
  }
  /**
   * getDetail
   * @param id string
   */
  getDetail(id: string): Observable<OrganizationDetailDto> {
    const _url = `/api/Organizations/${id}`;
    return this.request<OrganizationDetailDto>('get', _url);
  }
  /**
   * updateOrganization
   * @param id string
   * @param data OrganizationUpdateDto
   */
  updateOrganization(id: string, data: OrganizationUpdateDto): Observable<OrganizationDetailDto> {
    const _url = `/api/Organizations/${id}`;
    return this.request<OrganizationDetailDto>('put', _url, data);
  }
  /**
   * deleteOrganization
   * @param id string
   * @param hardDelete boolean
   */
  deleteOrganization(id: string, hardDelete: boolean): Observable<boolean> {
    const _url = `/api/Organizations/${id}?hardDelete=${hardDelete ?? ''}`;
    return this.request<boolean>('delete', _url);
  }
  /**
   * addUsers
   * @param id string
   * @param data string[]
   */
  addUsers(id: string, data: string[]): Observable<boolean> {
    const _url = `/api/Organizations/${id}/users`;
    return this.request<boolean>('post', _url, data);
  }
  /**
   * removeUsers
   * @param id string
   * @param data string[]
   */
  removeUsers(id: string, data: string[]): Observable<boolean> {
    const _url = `/api/Organizations/${id}/users`;
    return this.request<boolean>('delete', _url, data);
  }
}