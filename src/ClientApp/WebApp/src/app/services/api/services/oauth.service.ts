import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { TokenRequestDto } from '../models/iammod/token-request-dto.model';
import { DeviceAuthorizationRequestDto } from '../models/iammod/device-authorization-request-dto.model';
import { IntrospectRequestDto } from '../models/iammod/introspect-request-dto.model';
import { RevokeRequestDto } from '../models/iammod/revoke-request-dto.model';
import { LogoutRequestDto } from '../models/iammod/logout-request-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class OAuthService extends BaseService {
  /**
   * authorize
   * @param responseType string
   * @param clientId string
   * @param redirectUri string
   * @param scope string
   * @param state string
   * @param codeChallenge string
   * @param codeChallengeMethod string
   * @param responseMode string
   * @param nonce string
   * @param prompt string
   */
  authorize(responseType: string, clientId: string, redirectUri: string, scope: string | null, state: string | null, codeChallenge: string | null, codeChallengeMethod: string | null, responseMode: string | null, nonce: string | null, prompt: string | null): Observable<void> {
    const _url = `/connect/authorize?ResponseType=${responseType ?? ''}&ClientId=${clientId ?? ''}&RedirectUri=${redirectUri ?? ''}&Scope=${scope ?? ''}&State=${state ?? ''}&CodeChallenge=${codeChallenge ?? ''}&CodeChallengeMethod=${codeChallengeMethod ?? ''}&ResponseMode=${responseMode ?? ''}&Nonce=${nonce ?? ''}&Prompt=${prompt ?? ''}`;
    return this.request<void>('get', _url);
  }
  /**
   * token
   * @param data TokenRequestDto
   */
  token(data: TokenRequestDto): Observable<void> {
    const _url = `/connect/token`;
    return this.request<void>('post', _url, data);
  }
  /**
   * deviceAuthorization
   * @param data DeviceAuthorizationRequestDto
   */
  deviceAuthorization(data: DeviceAuthorizationRequestDto): Observable<void> {
    const _url = `/connect/device`;
    return this.request<void>('post', _url, data);
  }
  /**
   * introspect
   * @param data IntrospectRequestDto
   */
  introspect(data: IntrospectRequestDto): Observable<void> {
    const _url = `/connect/introspect`;
    return this.request<void>('post', _url, data);
  }
  /**
   * revoke
   * @param data RevokeRequestDto
   */
  revoke(data: RevokeRequestDto): Observable<void> {
    const _url = `/connect/revoke`;
    return this.request<void>('post', _url, data);
  }
  /**
   * logout
   * @param data LogoutRequestDto
   */
  logout(data: LogoutRequestDto): Observable<void> {
    const _url = `/connect/logout`;
    return this.request<void>('get', _url, data);
  }
  /**
   * userInfo
   */
  userInfo(): Observable<void> {
    const _url = `/connect/userinfo`;
    return this.request<void>('get', _url);
  }
}