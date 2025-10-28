import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/ater/page-list.model';
import { ScopeItemDto } from '../models/access-mod/scope-item-dto.model';
import { ScopeAddDto } from '../models/access-mod/scope-add-dto.model';
import { ScopeDetailDto } from '../models/access-mod/scope-detail-dto.model';
import { ScopeUpdateDto } from '../models/access-mod/scope-update-dto.model';
/**
 * API scope management controller
 */
@Injectable({ providedIn: 'root' })
export class ScopesService extends BaseService {
  /**
   * Get paged scopes
   * @param name Filter by scope name
   * @param displayName Filter by display name
   * @param required Filter by required flag
   * @param pageIndex number
   * @param pageSize number
   * @param orderBy Record<string, boolean>
   */
  getScopes(name: string | null, displayName: string | null, required: boolean | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<PageList<ScopeItemDto>> {
    const _url = `/api/Scopes?name=${name ?? ''}&displayName=${displayName ?? ''}&required=${required ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<ScopeItemDto>>('get', _url);
  }
  /**
   * Create new scope
   * @param data ScopeAddDto
   */
  createScope(data: ScopeAddDto): Observable<ScopeDetailDto> {
    const _url = `/api/Scopes`;
    return this.request<ScopeDetailDto>('post', _url, data);
  }
  /**
   * Get scope detail by id
   * @param id Scope id
   */
  getDetail(id: string): Observable<ScopeDetailDto> {
    const _url = `/api/Scopes/${id}`;
    return this.request<ScopeDetailDto>('get', _url);
  }
  /**
   * Update scope
   * @param id Scope id
   * @param data ScopeUpdateDto
   */
  updateScope(id: string, data: ScopeUpdateDto): Observable<ScopeDetailDto> {
    const _url = `/api/Scopes/${id}`;
    return this.request<ScopeDetailDto>('put', _url, data);
  }
  /**
   * Delete scope
   * @param id Scope id
   */
  deleteScope(id: string): Observable<any> {
    const _url = `/api/Scopes/${id}`;
    return this.request<any>('delete', _url);
  }
}