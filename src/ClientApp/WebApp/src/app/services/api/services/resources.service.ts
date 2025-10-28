import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/ater/page-list.model';
import { ResourceItemDto } from '../models/access-mod/resource-item-dto.model';
import { ResourceAddDto } from '../models/access-mod/resource-add-dto.model';
import { ResourceDetailDto } from '../models/access-mod/resource-detail-dto.model';
import { ResourceUpdateDto } from '../models/access-mod/resource-update-dto.model';
/**
 * API resource management controller
 */
@Injectable({ providedIn: 'root' })
export class ResourcesService extends BaseService {
  /**
   * Get paged resources
   * @param name Resource name filter
   * @param displayName Display name filter
   * @param pageIndex number
   * @param pageSize number
   * @param orderBy Record<string, boolean>
   */
  getResources(name: string | null, displayName: string | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<PageList<ResourceItemDto>> {
    const _url = `/api/Resources?name=${name ?? ''}&displayName=${displayName ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<ResourceItemDto>>('get', _url);
  }
  /**
   * Create new resource
   * @param data ResourceAddDto
   */
  createResource(data: ResourceAddDto): Observable<ResourceDetailDto> {
    const _url = `/api/Resources`;
    return this.request<ResourceDetailDto>('post', _url, data);
  }
  /**
   * Get resource detail by id
   * @param id Resource id
   */
  getDetail(id: string): Observable<ResourceDetailDto> {
    const _url = `/api/Resources/${id}`;
    return this.request<ResourceDetailDto>('get', _url);
  }
  /**
   * Update resource
   * @param id Resource id
   * @param data ResourceUpdateDto
   */
  updateResource(id: string, data: ResourceUpdateDto): Observable<ResourceDetailDto> {
    const _url = `/api/Resources/${id}`;
    return this.request<ResourceDetailDto>('put', _url, data);
  }
  /**
   * Delete resource
   * @param id Resource id
   */
  deleteResource(id: string): Observable<any> {
    const _url = `/api/Resources/${id}`;
    return this.request<any>('delete', _url);
  }
}