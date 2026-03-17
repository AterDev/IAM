import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseService } from './api/base.service';

export interface RegisterRequest {
  userName: string;
  email: string;
  phoneNumber?: string | null;
  password: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  code: string;
  newPassword: string;
}

export interface AccountMessageResponse {
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class AccountService extends BaseService {
  register(data: RegisterRequest): Observable<any> {
    return this.request<any>('post', '/api/Account/register', data);
  }

  requestPasswordReset(data: ForgotPasswordRequest): Observable<AccountMessageResponse> {
    return this.request<AccountMessageResponse>('post', '/api/Account/forgot-password', data);
  }

  resetPassword(data: ResetPasswordRequest): Observable<AccountMessageResponse> {
    return this.request<AccountMessageResponse>('post', '/api/Account/reset-password', data);
  }
}