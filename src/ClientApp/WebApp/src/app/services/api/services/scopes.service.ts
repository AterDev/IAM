import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/perigon/page-list.model';
import { ScopeItemDto } from '../models/iammod/scope-item-dto.model';
import { ScopeAddDto } from '../models/iammod/scope-add-dto.model';
import { ScopeDetailDto } from '../models/iammod/scope-detail-dto.model';
import { ScopeUpdateDto } from '../models/iammod/scope-update-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class ScopesService extends BaseService {
  /**
   * getScopes
   * @param name Filter by scope name
   * @param displayName Filter by display name
   * @param required Filter by required flag
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getScopes(name: string | null, displayName: string | null, required: boolean | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<ScopeItemDto>> {
    const _url = `/api/Scopes?Name=${name ?? ''}&DisplayName=${displayName ?? ''}&Required=${required ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<ScopeItemDto>>('get', _url);
  }
  /**
   * createScope
   * @param data ScopeAddDto
   */
  createScope(data: ScopeAddDto): Observable<ScopeDetailDto> {
    const _url = `/api/Scopes`;
    return this.request<ScopeDetailDto>('post', _url, data);
  }
  /**
   * getDetail
   * @param id string
   */
  getDetail(id: string): Observable<ScopeDetailDto> {
    const _url = `/api/Scopes/${id}`;
    return this.request<ScopeDetailDto>('get', _url);
  }
  /**
   * updateScope
   * @param id string
   * @param data ScopeUpdateDto
   */
  updateScope(id: string, data: ScopeUpdateDto): Observable<ScopeDetailDto> {
    const _url = `/api/Scopes/${id}`;
    return this.request<ScopeDetailDto>('put', _url, data);
  }
  /**
   * deleteScope
   * @param id string
   */
  deleteScope(id: string): Observable<boolean> {
    const _url = `/api/Scopes/${id}`;
    return this.request<boolean>('delete', _url);
  }
}