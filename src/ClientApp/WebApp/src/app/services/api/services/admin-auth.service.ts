import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminLoginDto } from '../models/iammod/admin-login-dto.model';
import { AdminLoginResponseDto } from '../models/iammod/admin-login-response-dto.model';
import { AdminUserInfo } from '../models/iammod/admin-user-info.model';
import { ApiMessageDto } from '../models/api-service/api-message-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class AdminAuthService extends BaseService {
  /**
   * login
   * @param data AdminLoginDto
   */
  login(data: AdminLoginDto): Observable<AdminLoginResponseDto> {
    const _url = `/api/admin/login`;
    return this.request<AdminLoginResponseDto>('post', _url, data);
  }
  /**
   * getCurrentUser
   */
  getCurrentUser(): Observable<AdminUserInfo> {
    const _url = `/api/admin/me`;
    return this.request<AdminUserInfo>('get', _url);
  }
  /**
   * logout
   */
  logout(): Observable<ApiMessageDto> {
    const _url = `/api/admin/logout`;
    return this.request<ApiMessageDto>('post', _url);
  }
}