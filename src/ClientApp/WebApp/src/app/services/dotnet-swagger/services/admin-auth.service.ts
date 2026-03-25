import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminLoginDto } from '../models/iammod/admin-login-dto.model';
import { AdminLoginResponseDto } from '../models/iammod/admin-login-response-dto.model';
import { AdminUserInfo } from '../models/iammod/admin-user-info.model';
/**
 * Admin authentication controller for management portal login
 */
@Injectable({ providedIn: 'root' })
export class AdminAuthService extends BaseService {
  /**
   * Admin login endpoint
   * @param data AdminLoginDto
   */
  login(data: AdminLoginDto): Observable<AdminLoginResponseDto> {
    const _url = `/api/admin/login`;
    return this.request<AdminLoginResponseDto>('post', _url, data);
  }
  /**
   * Get current admin user information
   */
  getCurrentUser(): Observable<AdminUserInfo> {
    const _url = `/api/admin/me`;
    return this.request<AdminUserInfo>('get', _url);
  }
  /**
   * Logout current admin user and revoke server-side session artifacts.
   */
  logout(): Observable<any> {
    const _url = `/api/admin/logout`;
    return this.request<any>('post', _url);
  }
}