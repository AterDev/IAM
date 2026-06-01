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
import { ClientRegistrationRequestDto } from '../models/iammod/client-registration-request-dto.model';
import { ClientRegistrationResultDto } from '../models/iammod/client-registration-result-dto.model';
import { ClientApprovalDto } from '../models/iammod/client-approval-dto.model';
import { ClientSecretDto } from '../models/iammod/client-secret-dto.model';
import { ClientSecretHistoryDto } from '../models/iammod/client-secret-history-dto.model';
import { ClientScopeAssignDto } from '../models/iammod/client-scope-assign-dto.model';
import { AuthorizationItemDto } from '../models/iammod/authorization-item-dto.model';
import { PermissionTreeNodeDto } from '../models/iammod/permission-tree-node-dto.model';
import { PermissionType } from '../models/entity/permission-type.model';
import { ClientPermissionSyncDto } from '../models/iammod/client-permission-sync-dto.model';
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
   * Register a new client for developer self-service review.
   * @param data ClientRegistrationRequestDto
   */
  registerClient(data: ClientRegistrationRequestDto): Observable<ClientRegistrationResultDto> {
    const _url = `/api/Clients/register`;
    return this.request<ClientRegistrationResultDto>('post', _url, data);
  }
  /**
   * Get clients visible to the current developer portal user.
   */
  getMyClients(): Observable<ClientDetailDto[]> {
    const _url = `/api/Clients/my-clients`;
    return this.request<ClientDetailDto[]>('get', _url);
  }
  /**
   * Get pending client registration requests.
   */
  getPendingRegistrations(): Observable<ClientDetailDto[]> {
    const _url = `/api/Clients/pending-registrations`;
    return this.request<ClientDetailDto[]>('get', _url);
  }
  /**
   * Approve a pending client registration.
   * @param id string
   * @param data ClientApprovalDto
   */
  approveClient(id: string, data: ClientApprovalDto): Observable<ClientRegistrationResultDto> {
    const _url = `/api/Clients/${id}/approve`;
    return this.request<ClientRegistrationResultDto>('post', _url, data);
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
   * Get client secret history metadata.
   * @param id string
   */
  getSecrets(id: string): Observable<ClientSecretHistoryDto[]> {
    const _url = `/api/Clients/${id}/secrets`;
    return this.request<ClientSecretHistoryDto[]>('get', _url);
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
  /**
   * Replace client permission relations.
   * @param id string
   * @param data string[]
   */
  assignPermissions(id: string, data: string[]): Observable<any> {
    const _url = `/api/Clients/${id}/permissions`;
    return this.request<any>('post', _url, data);
  }
  /**
   * Get client permission codes.
   * @param id string
   */
  getPermissions(id: string): Observable<string[]> {
    const _url = `/api/Clients/${id}/permissions`;
    return this.request<string[]>('get', _url);
  }
  /**
   * Get client permission tree.
   * @param id string
   * @param clientId Filter by database client id.
   * @param clientCode Filter by public client identifier.
   * @param type Filter by permission type.
   * @param parentId Filter by parent permission id.
   * @param keyword Filter by keyword.
   * @param onlyNonBusiness Whether to include only menu and button permissions.
   * @param pageIndex number
   * @param pageSize number
   * @param orderBy Record<string, boolean>
   */
  getPermissionTree(id: string, clientId: string | null, clientCode: string | null, type: PermissionType | null, parentId: string | null, keyword: string | null, onlyNonBusiness: boolean | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<PermissionTreeNodeDto[]> {
    const _url = `/api/Clients/${id}/permission-tree?clientId=${clientId ?? ''}&clientCode=${clientCode ?? ''}&type=${type ?? ''}&parentId=${parentId ?? ''}&keyword=${keyword ?? ''}&onlyNonBusiness=${onlyNonBusiness ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PermissionTreeNodeDto[]>('get', _url);
  }
  /**
   * Full replacement synchronization for client menu/button permissions.
   * @param id string
   * @param data ClientPermissionSyncDto
   */
  syncMenuPermissions(id: string, data: ClientPermissionSyncDto): Observable<any> {
    const _url = `/api/Clients/${id}/menu-permissions:sync`;
    return this.request<any>('post', _url, data);
  }
}