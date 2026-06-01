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
   * @param responseType Response type. Currently only authorization code flow is supported.
   * @param clientId Client identifier
   * @param redirectUri Redirect URI
   * @param scope Requested scopes (space-separated)
   * @param state State parameter for CSRF protection
   * @param codeChallenge PKCE code challenge
   * @param codeChallengeMethod PKCE code challenge method (plain, S256)
   * @param responseMode Response mode. Currently only query mode is supported.
   * @param nonce Nonce for OIDC
   * @param prompt Prompt parameter (none, login, consent, select_account)
   */
  authorize(responseType: string, clientId: string, redirectUri: string, scope: string | null, state: string | null, codeChallenge: string | null, codeChallengeMethod: string | null, responseMode: string | null, nonce: string | null, prompt: string | null): Observable<any> {
    const _url = `/connect/authorize?response_type=${responseType ?? ''}&client_id=${clientId ?? ''}&redirect_uri=${redirectUri ?? ''}&scope=${scope ?? ''}&state=${state ?? ''}&code_challenge=${codeChallenge ?? ''}&code_challenge_method=${codeChallengeMethod ?? ''}&response_mode=${responseMode ?? ''}&nonce=${nonce ?? ''}&prompt=${prompt ?? ''}`;
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
   * @param idTokenHint ID token hint
   * @param postLogoutRedirectUri Post logout redirect URI
   * @param state State parameter
   */
  logout(idTokenHint: string | null, postLogoutRedirectUri: string | null, state: string | null): Observable<any> {
    const _url = `/connect/logout?id_token_hint=${idTokenHint ?? ''}&post_logout_redirect_uri=${postLogoutRedirectUri ?? ''}&state=${state ?? ''}`;
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