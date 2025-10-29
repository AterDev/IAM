import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminLoginDto } from '../models/identity-mod/admin-login-dto.model';
import { AdminLoginResponseDto } from '../models/identity-mod/admin-login-response-dto.model';
import { AdminUserInfo } from '../models/identity-mod/admin-user-info.model';
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
}