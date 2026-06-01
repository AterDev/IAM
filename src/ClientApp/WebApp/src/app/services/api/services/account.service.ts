import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { RegisterRequestDto } from '../models/iammod/register-request-dto.model';
import { UserDetailDto } from '../models/iammod/user-detail-dto.model';
import { ForgotPasswordRequestDto } from '../models/iammod/forgot-password-request-dto.model';
import { ResetPasswordRequestDto } from '../models/iammod/reset-password-request-dto.model';
import { ChangePasswordRequestDto } from '../models/iammod/change-password-request-dto.model';
import { MfaStatusDto } from '../models/iammod/mfa-status-dto.model';
import { MfaSetupResponseDto } from '../models/iammod/mfa-setup-response-dto.model';
import { EnableMfaRequestDto } from '../models/iammod/enable-mfa-request-dto.model';
import { MfaRecoveryCodesResponseDto } from '../models/iammod/mfa-recovery-codes-response-dto.model';
import { DisableMfaRequestDto } from '../models/iammod/disable-mfa-request-dto.model';
import { RegenerateRecoveryCodesRequestDto } from '../models/iammod/regenerate-recovery-codes-request-dto.model';
/**
 * Self-service account endpoints for public authentication flows.
 */
@Injectable({ providedIn: 'root' })
export class AccountService extends BaseService {
  /**
   * register
   * @param data RegisterRequestDto
   */
  register(data: RegisterRequestDto): Observable<UserDetailDto> {
    const _url = `/api/Account/register`;
    return this.request<UserDetailDto>('post', _url, data);
  }
  /**
   * forgotPassword
   * @param data ForgotPasswordRequestDto
   */
  forgotPassword(data: ForgotPasswordRequestDto): Observable<any> {
    const _url = `/api/Account/forgot-password`;
    return this.request<any>('post', _url, data);
  }
  /**
   * resetPassword
   * @param data ResetPasswordRequestDto
   */
  resetPassword(data: ResetPasswordRequestDto): Observable<any> {
    const _url = `/api/Account/reset-password`;
    return this.request<any>('post', _url, data);
  }
  /**
   * changePassword
   * @param data ChangePasswordRequestDto
   */
  changePassword(data: ChangePasswordRequestDto): Observable<any> {
    const _url = `/api/Account/change-password`;
    return this.request<any>('post', _url, data);
  }
  /**
   * getMfaStatus
   */
  getMfaStatus(): Observable<MfaStatusDto> {
    const _url = `/api/Account/mfa`;
    return this.request<MfaStatusDto>('get', _url);
  }
  /**
   * beginMfaSetup
   */
  beginMfaSetup(): Observable<MfaSetupResponseDto> {
    const _url = `/api/Account/mfa/setup`;
    return this.request<MfaSetupResponseDto>('post', _url);
  }
  /**
   * enableMfa
   * @param data EnableMfaRequestDto
   */
  enableMfa(data: EnableMfaRequestDto): Observable<MfaRecoveryCodesResponseDto> {
    const _url = `/api/Account/mfa/enable`;
    return this.request<MfaRecoveryCodesResponseDto>('post', _url, data);
  }
  /**
   * disableMfa
   * @param data DisableMfaRequestDto
   */
  disableMfa(data: DisableMfaRequestDto): Observable<any> {
    const _url = `/api/Account/mfa/disable`;
    return this.request<any>('post', _url, data);
  }
  /**
   * regenerateRecoveryCodes
   * @param data RegenerateRecoveryCodesRequestDto
   */
  regenerateRecoveryCodes(data: RegenerateRecoveryCodesRequestDto): Observable<MfaRecoveryCodesResponseDto> {
    const _url = `/api/Account/mfa/recovery-codes/regenerate`;
    return this.request<MfaRecoveryCodesResponseDto>('post', _url, data);
  }
}