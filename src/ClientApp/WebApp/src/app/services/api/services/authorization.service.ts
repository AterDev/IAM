import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserAuthorizationDto } from '../models/iammod/user-authorization-dto.model';
/**
 * Authorization management controller for users to view and manage their authorizations
 */
@Injectable({ providedIn: 'root' })
export class AuthorizationService extends BaseService {
  /**
   * Get current user's authorizations
   */
  getUserAuthorizations(): Observable<UserAuthorizationDto[]> {
    const _url = `/api/Authorization`;
    return this.request<UserAuthorizationDto[]>('get', _url);
  }
  /**
   * Revoke a specific authorization
   * @param id Authorization ID
   */
  revokeAuthorization(id: string): Observable<any> {
    const _url = `/api/Authorization/${id}`;
    return this.request<any>('delete', _url);
  }
  /**
   * Revoke all authorizations for a specific client
   * @param clientId Client ID
   */
  revokeClientAuthorizations(clientId: string): Observable<any> {
    const _url = `/api/Authorization/client/${clientId}`;
    return this.request<any>('delete', _url);
  }
}