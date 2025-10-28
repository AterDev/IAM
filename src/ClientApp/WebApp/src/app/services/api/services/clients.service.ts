import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ClientAddDto } from '../models/access-mod/client-add-dto.model';
import { ClientUpdateDto } from '../models/access-mod/client-update-dto.model';
import { ClientScopeAssignDto } from '../models/access-mod/client-scope-assign-dto.model';
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
  getClients(clientId: string | null, displayName: string | null, type: string | null, applicationType: string | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<ProblemDetails> {
    const _url = `/api/Clients?clientId=${clientId ?? ''}&displayName=${displayName ?? ''}&type=${type ?? ''}&applicationType=${applicationType ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<ProblemDetails>('get', _url);
  }
  /**
   * Create new client
   * @param data ClientAddDto
   */
  createClient(data: ClientAddDto): Observable<ProblemDetails> {
    const _url = `/api/Clients`;
    return this.request<ProblemDetails>('post', _url, data);
  }
  /**
   * Get client detail by id
   * @param id Client id
   */
  getDetail(id: string): Observable<ProblemDetails> {
    const _url = `/api/Clients/${id}`;
    return this.request<ProblemDetails>('get', _url);
  }
  /**
   * Update client
   * @param id Client id
   * @param data ClientUpdateDto
   */
  updateClient(id: string, data: ClientUpdateDto): Observable<ProblemDetails> {
    const _url = `/api/Clients/${id}`;
    return this.request<ProblemDetails>('put', _url, data);
  }
  /**
   * Delete client
   * @param id Client id
   */
  deleteClient(id: string): Observable<ProblemDetails> {
    const _url = `/api/Clients/${id}`;
    return this.request<ProblemDetails>('delete', _url);
  }
  /**
   * Rotate client secret
   * @param id Client unique identifier
   */
  rotateSecret(id: string): Observable<ProblemDetails> {
    const _url = `/api/Clients/${id}/secret:rotate`;
    return this.request<ProblemDetails>('post', _url);
  }
  /**
   * Assign scopes to client
   * @param id Client id
   * @param data ClientScopeAssignDto
   */
  assignScopes(id: string, data: ClientScopeAssignDto): Observable<ProblemDetails> {
    const _url = `/api/Clients/${id}/scopes`;
    return this.request<ProblemDetails>('post', _url, data);
  }
  /**
   * Get client authorizations
   * @param id Client id
   */
  getAuthorizations(id: string): Observable<ProblemDetails> {
    const _url = `/api/Clients/${id}/authorizations`;
    return this.request<ProblemDetails>('get', _url);
  }
}