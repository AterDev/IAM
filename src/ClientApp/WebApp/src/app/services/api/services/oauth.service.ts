import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
/**
 * OAuth/OIDC Connect endpoints controller
 */
@Injectable({ providedIn: 'root' })
export class OAuthService extends BaseService {
  /**
   * Authorization endpoint (OAuth 2.0 / OIDC) - POST method
   * @param responseType Response type (code, token, id_token)
   * @param clientId Client identifier
   * @param redirectUri Redirect URI
   * @param scope Requested scopes (space-separated)
   * @param state State parameter for CSRF protection
   * @param codeChallenge PKCE code challenge
   * @param codeChallengeMethod PKCE code challenge method (plain, S256)
   * @param responseMode Response mode (query, fragment, form_post)
   * @param nonce Nonce for OIDC
   * @param prompt Prompt parameter (none, login, consent, select_account)
   */
  authorize(responseType: string, clientId: string, redirectUri: string, scope: string | null, state: string | null, codeChallenge: string | null, codeChallengeMethod: string | null, responseMode: string | null, nonce: string | null, prompt: string | null): Observable<any> {
    const _url = `/connect/authorize?responseType=${responseType ?? ''}&clientId=${clientId ?? ''}&redirectUri=${redirectUri ?? ''}&scope=${scope ?? ''}&state=${state ?? ''}&codeChallenge=${codeChallenge ?? ''}&codeChallengeMethod=${codeChallengeMethod ?? ''}&responseMode=${responseMode ?? ''}&nonce=${nonce ?? ''}&prompt=${prompt ?? ''}`;
    return this.request<any>('post', _url);
  }
  /**
   * Token endpoint (OAuth 2.0 / OIDC)
   * @param data any
   */
  token(data: any): Observable<any> {
    const _url = `/connect/token`;
    return this.request<any>('post', _url, data);
  }
  /**
   * Device authorization endpoint (RFC 8628)
   * @param data any
   */
  deviceAuthorization(data: any): Observable<any> {
    const _url = `/connect/device`;
    return this.request<any>('post', _url, data);
  }
  /**
   * Token introspection endpoint (RFC 7662)
   * @param data any
   */
  introspect(data: any): Observable<any> {
    const _url = `/connect/introspect`;
    return this.request<any>('post', _url, data);
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
   * Logout endpoint (OIDC) - POST method
   * @param idTokenHint ID token hint
   * @param postLogoutRedirectUri Post logout redirect URI
   * @param state State parameter
   */
  logout(idTokenHint: string | null, postLogoutRedirectUri: string | null, state: string | null): Observable<any> {
    const _url = `/connect/logout?idTokenHint=${idTokenHint ?? ''}&postLogoutRedirectUri=${postLogoutRedirectUri ?? ''}&state=${state ?? ''}`;
    return this.request<any>('post', _url);
  }
}