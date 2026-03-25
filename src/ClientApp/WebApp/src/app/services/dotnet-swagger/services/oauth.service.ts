import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { TokenResponseDto } from '../models/iammod/token-response-dto.model';
import { DeviceAuthorizationResponseDto } from '../models/iammod/device-authorization-response-dto.model';
import { IntrospectResponseDto } from '../models/iammod/introspect-response-dto.model';
import { UserInfoDto } from '../models/iammod/user-info-dto.model';
/**
 * OAuth 2.0 / OpenID Connect endpoint controller
 */
@Injectable({ providedIn: 'root' })
export class OAuthService extends BaseService {
  /**
   * Authorization endpoint (OAuth 2.0 / OIDC)
   * @param response_type Response type. Currently only authorization code flow is supported.
   * @param client_id Client identifier
   * @param redirect_uri Redirect URI
   * @param scope Requested scopes (space-separated)
   * @param state State parameter for CSRF protection
   * @param code_challenge PKCE code challenge
   * @param code_challenge_method PKCE code challenge method (plain, S256)
   * @param response_mode Response mode. Currently only query mode is supported.
   * @param nonce Nonce for OIDC
   * @param prompt Prompt parameter (none, login, consent, select_account)
   */
  authorize(response_type: string, client_id: string, redirect_uri: string, scope: string | null, state: string | null, code_challenge: string | null, code_challenge_method: string | null, response_mode: string | null, nonce: string | null, prompt: string | null): Observable<any> {
    const _url = `/connect/authorize?response_type=${response_type ?? ''}&client_id=${client_id ?? ''}&redirect_uri=${redirect_uri ?? ''}&scope=${scope ?? ''}&state=${state ?? ''}&code_challenge=${code_challenge ?? ''}&code_challenge_method=${code_challenge_method ?? ''}&response_mode=${response_mode ?? ''}&nonce=${nonce ?? ''}&prompt=${prompt ?? ''}`;
    return this.request<any>('get', _url);
  }
  /**
   * Token endpoint (OAuth 2.0 / OIDC)
   * @param data any
   */
  token(data: any): Observable<TokenResponseDto> {
    const _url = `/connect/token`;
    return this.request<TokenResponseDto>('post', _url, data);
  }
  /**
   * Device authorization endpoint (RFC 8628)
   * @param data any
   */
  deviceAuthorization(data: any): Observable<DeviceAuthorizationResponseDto> {
    const _url = `/connect/device`;
    return this.request<DeviceAuthorizationResponseDto>('post', _url, data);
  }
  /**
   * Token introspection endpoint (RFC 7662)
   * @param data any
   */
  introspect(data: any): Observable<IntrospectResponseDto> {
    const _url = `/connect/introspect`;
    return this.request<IntrospectResponseDto>('post', _url, data);
  }
  /**
   * Token revocation endpoint (RFC 7009)
   * @param data any
   */
  revoke(data: any): Observable<any> {
    const _url = `/connect/revoke`;
    return this.request<any>('post', _url, data);
  }
  /**
   * Logout endpoint (OIDC)
   * @param id_token_hint ID token hint
   * @param post_logout_redirect_uri Post logout redirect URI
   * @param state State parameter
   */
  logout(id_token_hint: string | null, post_logout_redirect_uri: string | null, state: string | null): Observable<any> {
    const _url = `/connect/logout?id_token_hint=${id_token_hint ?? ''}&post_logout_redirect_uri=${post_logout_redirect_uri ?? ''}&state=${state ?? ''}`;
    return this.request<any>('get', _url);
  }
  /**
   * UserInfo endpoint (OIDC)
   */
  userInfo(): Observable<UserInfoDto> {
    const _url = `/connect/userinfo`;
    return this.request<UserInfoDto>('get', _url);
  }
}