import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseService } from './api/base.service';
import { HttpClient } from '@angular/common/http';

export interface MfaStatus {
  isEnabled: boolean;
  hasPendingSetup: boolean;
  recoveryCodesRemaining: number;
  canRegenerateRecoveryCodes: boolean;
}

export interface MfaSetupResponse {
  secret: string;
  otpAuthUri: string;
  issuer: string;
  accountName: string;
}

export interface MfaRecoveryCodesResponse {
  recoveryCodes: string[];
}

@Injectable({ providedIn: 'root' })
export class MfaService extends BaseService {
  constructor(http: HttpClient, @Inject('API_BASE_URL') baseUrl: string) {
    super(http, baseUrl);
  }

  getStatus(): Observable<MfaStatus> {
    return this.request<MfaStatus>('get', '/api/Account/mfa');
  }

  beginSetup(): Observable<MfaSetupResponse> {
    return this.request<MfaSetupResponse>('post', '/api/Account/mfa/setup');
  }

  enable(code: string): Observable<MfaRecoveryCodesResponse> {
    return this.request<MfaRecoveryCodesResponse>('post', '/api/Account/mfa/enable', { code });
  }

  disable(code: string): Observable<unknown> {
    return this.request('post', '/api/Account/mfa/disable', { code });
  }

  regenerateRecoveryCodes(code: string): Observable<MfaRecoveryCodesResponse> {
    return this.request<MfaRecoveryCodesResponse>('post', '/api/Account/mfa/recovery-codes/regenerate', { code });
  }
}
