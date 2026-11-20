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
 *
 */
@Injectable({ providedIn: 'root' })
export class ClientsService extends BaseService {
  /**
   * getClients
   * @param clientId Filter by client ID
   * @param displayName Filter by display name
   * @param type Filter by client type
   * @param applicationType Filter by application type
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getClients(clientId: string | null, displayName: string | null, type: ClientType | null, applicationType: ApplicationType | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<ClientItemDto>> {
    const _url = `/api/Clients?ClientId=${clientId ?? ''}&DisplayName=${displayName ?? ''}&Type=${type ?? ''}&ApplicationType=${applicationType ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<ClientItemDto>>('get', _url);
  }
  /**
   * createClient
   * @param data ClientAddDto
   */
  createClient(data: ClientAddDto): Observable<any> {
    const _url = `/api/Clients`;
    return this.request<any>('post', _url, data);
  }
  /**
   * getDetail
   * @param id string
   */
  getDetail(id: string): Observable<ClientDetailDto> {
    const _url = `/api/Clients/${id}`;
    return this.request<ClientDetailDto>('get', _url);
  }
  /**
   * updateClient
   * @param id string
   * @param data ClientUpdateDto
   */
  updateClient(id: string, data: ClientUpdateDto): Observable<ClientDetailDto> {
    const _url = `/api/Clients/${id}`;
    return this.request<ClientDetailDto>('put', _url, data);
  }
  /**
   * deleteClient
   * @param id string
   */
  deleteClient(id: string): Observable<boolean> {
    const _url = `/api/Clients/${id}`;
    return this.request<boolean>('delete', _url);
  }
  /**
   * registerClient
   * @param data ClientRegistrationRequestDto
   */
  registerClient(data: ClientRegistrationRequestDto): Observable<ClientRegistrationResultDto> {
    const _url = `/api/Clients/register`;
    return this.request<ClientRegistrationResultDto>('post', _url, data);
  }
  /**
   * getMyClients
   */
  getMyClients(): Observable<ClientDetailDto[]> {
    const _url = `/api/Clients/my-clients`;
    return this.request<ClientDetailDto[]>('get', _url);
  }
  /**
   * getPendingRegistrations
   */
  getPendingRegistrations(): Observable<ClientDetailDto[]> {
    const _url = `/api/Clients/pending-registrations`;
    return this.request<ClientDetailDto[]>('get', _url);
  }
  /**
   * approveClient
   * @param id string
   * @param data any
   */
  approveClient(id: string, data: any): Observable<ClientRegistrationResultDto> {
    const _url = `/api/Clients/${id}/approve`;
    return this.request<ClientRegistrationResultDto>('post', _url, data);
  }
  /**
   * rotateSecret
   * @param id string
   */
  rotateSecret(id: string): Observable<ClientSecretDto> {
    const _url = `/api/Clients/${id}/secret:rotate`;
    return this.request<ClientSecretDto>('post', _url);
  }
  /**
   * getSecrets
   * @param id string
   */
  getSecrets(id: string): Observable<ClientSecretHistoryDto[]> {
    const _url = `/api/Clients/${id}/secrets`;
    return this.request<ClientSecretHistoryDto[]>('get', _url);
  }
  /**
   * assignScopes
   * @param id string
   * @param data ClientScopeAssignDto
   */
  assignScopes(id: string, data: ClientScopeAssignDto): Observable<boolean> {
    const _url = `/api/Clients/${id}/scopes`;
    return this.request<boolean>('post', _url, data);
  }
  /**
   * getAuthorizations
   * @param id string
   */
  getAuthorizations(id: string): Observable<AuthorizationItemDto[]> {
    const _url = `/api/Clients/${id}/authorizations`;
    return this.request<AuthorizationItemDto[]>('get', _url);
  }
  /**
   * assignPermissions
   * @param id string
   * @param data string[]
   */
  assignPermissions(id: string, data: string[]): Observable<boolean> {
    const _url = `/api/Clients/${id}/permissions`;
    return this.request<boolean>('post', _url, data);
  }
  /**
   * getPermissions
   * @param id string
   */
  getPermissions(id: string): Observable<string[]> {
    const _url = `/api/Clients/${id}/permissions`;
    return this.request<string[]>('get', _url);
  }
  /**
   * getPermissionTree
   * @param id string
   * @param clientId Filter by database client id.
   * @param clientCode Filter by public client identifier.
   * @param type Filter by permission type.
   * @param parentId Filter by parent permission id.
   * @param keyword Filter by keyword.
   * @param onlyNonBusiness Whether to include only menu and button permissions.
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getPermissionTree(id: string, clientId: string | null, clientCode: string | null, type: PermissionType | null, parentId: string | null, keyword: string | null, onlyNonBusiness: boolean | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PermissionTreeNodeDto[]> {
    const _url = `/api/Clients/${id}/permission-tree?ClientId=${clientId ?? ''}&ClientCode=${clientCode ?? ''}&Type=${type ?? ''}&ParentId=${parentId ?? ''}&Keyword=${keyword ?? ''}&OnlyNonBusiness=${onlyNonBusiness ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PermissionTreeNodeDto[]>('get', _url);
  }
  /**
   * syncMenuPermissions
   * @param id string
   * @param data ClientPermissionSyncDto
   */
  syncMenuPermissions(id: string, data: ClientPermissionSyncDto): Observable<boolean> {
    const _url = `/api/Clients/${id}/menu-permissions:sync`;
    return this.request<boolean>('post', _url, data);
  }
}