import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ResourceItemDto } from '../models/iammod/resource-item-dto.model';
import { PageList } from '../models/perigon/page-list.model';
import { ResourceAddDto } from '../models/iammod/resource-add-dto.model';
import { ResourceDetailDto } from '../models/iammod/resource-detail-dto.model';
import { ResourceUpdateDto } from '../models/iammod/resource-update-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class ResourcesService extends BaseService {
  /**
   * getAll
   */
  getAll(): Observable<ResourceItemDto[]> {
    const _url = `/api/Resources/all`;
    return this.request<ResourceItemDto[]>('get', _url);
  }
  /**
   * getResources
   * @param name Resource name filter
   * @param displayName Display name filter
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getResources(name: string | null, displayName: string | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<ResourceItemDto>> {
    const _url = `/api/Resources?Name=${name ?? ''}&DisplayName=${displayName ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<ResourceItemDto>>('get', _url);
  }
  /**
   * createResource
   * @param data ResourceAddDto
   */
  createResource(data: ResourceAddDto): Observable<ResourceDetailDto> {
    const _url = `/api/Resources`;
    return this.request<ResourceDetailDto>('post', _url, data);
  }
  /**
   * getDetail
   * @param id string
   */
  getDetail(id: string): Observable<ResourceDetailDto> {
    const _url = `/api/Resources/${id}`;
    return this.request<ResourceDetailDto>('get', _url);
  }
  /**
   * updateResource
   * @param id string
   * @param data ResourceUpdateDto
   */
  updateResource(id: string, data: ResourceUpdateDto): Observable<ResourceDetailDto> {
    const _url = `/api/Resources/${id}`;
    return this.request<ResourceDetailDto>('put', _url, data);
  }
  /**
   * deleteResource
   * @param id string
   */
  deleteResource(id: string): Observable<boolean> {
    const _url = `/api/Resources/${id}`;
    return this.request<boolean>('delete', _url);
  }
}