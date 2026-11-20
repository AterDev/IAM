import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserAuthorizationDto } from '../models/iammod/user-authorization-dto.model';
import { ApiMessageDto } from '../models/api-service/api-message-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class AuthorizationService extends BaseService {
  /**
   * getUserAuthorizations
   */
  getUserAuthorizations(): Observable<UserAuthorizationDto[]> {
    const _url = `/api/Authorization`;
    return this.request<UserAuthorizationDto[]>('get', _url);
  }
  /**
   * revokeAuthorization
   * @param id string
   */
  revokeAuthorization(id: string): Observable<ApiMessageDto> {
    const _url = `/api/Authorization/${id}`;
    return this.request<ApiMessageDto>('delete', _url);
  }
  /**
   * revokeClientAuthorizations
   * @param clientId string
   */
  revokeClientAuthorizations(clientId: string): Observable<ApiMessageDto> {
    const _url = `/api/Authorization/client/${clientId}`;
    return this.request<ApiMessageDto>('delete', _url);
  }
}