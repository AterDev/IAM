import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class ExternalAuthService extends BaseService {
  /**
   * signInMicrosoft
   * @param returnUrl string
   */
  signInMicrosoft(returnUrl: string | null): Observable<void> {
    const _url = `/api/ExternalAuth/signin-microsoft?returnUrl=${returnUrl ?? ''}`;
    return this.request<void>('get', _url);
  }
  /**
   * signInGoogle
   * @param returnUrl string
   */
  signInGoogle(returnUrl: string | null): Observable<void> {
    const _url = `/api/ExternalAuth/signin-google?returnUrl=${returnUrl ?? ''}`;
    return this.request<void>('get', _url);
  }
  /**
   * getToken
   * @param type string
   * @param returnUrl string
   */
  getToken(type: string, returnUrl: string | null): Observable<void> {
    const _url = `/api/ExternalAuth/getToken?type=${type ?? ''}&returnUrl=${returnUrl ?? ''}`;
    return this.request<void>('get', _url);
  }
}