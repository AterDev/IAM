import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/perigon/page-list.model';
import { ClientType } from '../models/entity/client-type.model';
import { ApplicationType } from '../models/entity/application-type.model';
import { ClientItemDto } from '../models/iammod/client-item-dto.model';
import { ClientAddDto } from '../models/iammod/client-add-dto.model';
import { ClientDetailDto } from '../models/iammod/client-detail-dto.model';
import { ClientUpdateDto } from '../models/iammod/client-update-dto.model';
import { ClientSecretDto } from '../models/iammod/client-secret-dto.model';
import { ClientScopeAssignDto } from '../models/iammod/client-scope-assign-dto.model';
import { AuthorizationItemDto } from '../models/iammod/authorization-item-dto.model';
/**
 * OAuth/OIDC client management controller
 */
@Injectable({ providedIn: 'root' })
export class ClientsService extends BaseService {
  /**
   * Get paged clients
   * @param clientId Filter by client ID
   * @param displayName Filter by display name
   * @param type Filter by client type
   * @param applicationType Filter by application type
   * @param pageIndex number
   * @param pageSize number
   * @param orderBy Record<string, boolean>
   */
  getClients(clientId: string | null, displayName: string | null, type: ClientType | null, applicationType: ApplicationType | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<PageList<ClientItemDto>> {
    const _url = `/api/Clients?clientId=${clientId ?? ''}&displayName=${displayName ?? ''}&type=${type ?? ''}&applicationType=${applicationType ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<ClientItemDto>>('get', _url);
  }
  /**
   * Create new client
   * @param data ClientAddDto
   */
  createClient(data: ClientAddDto): Observable<string> {
    const _url = `/api/Clients`;
    return this.request<string>('post', _url, data);
  }
  /**
   * Get client detail by id
   * @param id Client id
   */
  getDetail(id: string): Observable<ClientDetailDto> {
    const _url = `/api/Clients/${id}`;
    return this.request<ClientDetailDto>('get', _url);
  }
  /**
   * Update client
   * @param id Client id
   * @param data ClientUpdateDto
   */
  updateClient(id: string, data: ClientUpdateDto): Observable<ClientDetailDto> {
    const _url = `/api/Clients/${id}`;
    return this.request<ClientDetailDto>('put', _url, data);
  }
  /**
   * Delete client
   * @param id Client id
   */
  deleteClient(id: string): Observable<any> {
    const _url = `/api/Clients/${id}`;
    return this.request<any>('delete', _url);
  }
  /**
   * Rotate client secret
   * @param id Client unique identifier
   */
  rotateSecret(id: string): Observable<ClientSecretDto> {
    const _url = `/api/Clients/${id}/secret:rotate`;
    return this.request<ClientSecretDto>('post', _url);
  }
  /**
   * Assign scopes to client
   * @param id Client id
   * @param data ClientScopeAssignDto
   */
  assignScopes(id: string, data: ClientScopeAssignDto): Observable<any> {
    const _url = `/api/Clients/${id}/scopes`;
    return this.request<any>('post', _url, data);
  }
  /**
   * Get client authorizations
   * @param id Client id
   */
  getAuthorizations(id: string): Observable<AuthorizationItemDto[]> {
    const _url = `/api/Clients/${id}/authorizations`;
    return this.request<AuthorizationItemDto[]>('get', _url);
  }
}