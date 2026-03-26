import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TokenResponseDto } from './api/models/iammod/token-response-dto.model';

const RETURN_URL_STORAGE_KEY = 'iam.admin.returnUrl';
const CODE_VERIFIER_STORAGE_KEY = 'iam.admin.codeVerifier';
const OIDC_STATE_STORAGE_KEY = 'iam.admin.oidcState';
const ID_TOKEN_STORAGE_KEY = 'iam.admin.idToken';
const REFRESH_TOKEN_STORAGE_KEY = 'iam.admin.refreshToken';
const OIDC_CLIENT_ID = 'AdminWebClient';
const OIDC_SCOPE = 'openid profile email offline_access';
const SUPER_ADMIN_ROLE = 'SuperAdmin';
const ADMIN_USER_ROLE = 'AdminUser';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  isLogin = false;
  isAdmin = false;
  userName?: string | null = null;
  id?: string | null = null;
  sessionId?: string | null = null;
  readonly sessionExpired = signal(false);

  constructor(
    private readonly http: HttpClient,
    @Inject('API_BASE_URL') private readonly baseUrl: string,
  ) {
    this.updateUserLoginState();
  }

  saveLoginState(username: string, token: string, sessionId?: string | null): void {
    this.isLogin = true;
    this.sessionExpired.set(false);
    this.userName = username;
    this.sessionId = sessionId ?? null;
    this.isAdmin = this.hasAdminRole(this.getRoles(token));
    localStorage.setItem("username", username);
    localStorage.setItem("accessToken", token);
    if (sessionId) {
      localStorage.setItem('sessionId', sessionId);
    } else {
      localStorage.removeItem('sessionId');
    }
  }

  private getAuthorityUrl(): string {
    const currentUrl = new URL(window.location.origin);

    if (currentUrl.hostname === 'localhost' && (currentUrl.port === '4200' || currentUrl.port === '4201')) {
      return `https://${currentUrl.hostname}:9900`;
    }

    return currentUrl.origin;
  }

  private getRedirectUrl(): string {
    return `${window.location.origin}/auth/callback`;
  }

  private createRandomString(bytes = 32): string {
    const buffer = new Uint8Array(bytes);
    crypto.getRandomValues(buffer);
    return this.toBase64Url(buffer);
  }

  private toBase64Url(buffer: Uint8Array): string {
    let binary = '';
    buffer.forEach((byte) => {
      binary += String.fromCharCode(byte);
    });

    return btoa(binary)
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/g, '');
  }

  private async buildPkceChallenge(verifier: string): Promise<string> {
    const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier));
    return this.toBase64Url(new Uint8Array(digest));
  }

  private buildAuthorizeUrl(codeChallenge: string, state: string): string {
    const url = new URL(`${this.getAuthorityUrl()}/connect/authorize`);
    url.searchParams.set('response_type', 'code');
    url.searchParams.set('client_id', OIDC_CLIENT_ID);
    url.searchParams.set('redirect_uri', this.getRedirectUrl());
    url.searchParams.set('scope', OIDC_SCOPE);
    url.searchParams.set('state', state);
    url.searchParams.set('code_challenge', codeChallenge);
    url.searchParams.set('code_challenge_method', 'S256');
    return url.toString();
  }

  private pickUserName(userData: Record<string, unknown> | null | undefined): string | null {
    const candidates = [
      userData?.['preferred_username'],
      userData?.['name'],
      userData?.['email'],
      userData?.['sub'],
    ];

    for (const candidate of candidates) {
      if (typeof candidate === 'string' && candidate.trim().length > 0) {
        return candidate;
      }
    }

    return localStorage.getItem('username');
  }

  private extractJwtClaim(token: string, claimName: string): string | null {
    const claims = this.readJwtPayload(token);
    if (!claims) {
      return null;
    }

    const value = claims[claimName];
    return typeof value === 'string' ? value : null;
  }

  private extractJwtClaims(token: string, claimName: string): string[] {
    const claims = this.readJwtPayload(token);
    if (!claims) {
      return [];
    }

    const value = claims[claimName];
    if (typeof value === 'string' && value.trim().length > 0) {
      return [value];
    }

    if (Array.isArray(value)) {
      return value.filter((item): item is string => typeof item === 'string' && item.trim().length > 0);
    }

    return [];
  }

  private readJwtPayload(token: string): Record<string, unknown> | null {
    try {
      const payload = token.split('.')[1];
      if (!payload) {
        return null;
      }

      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const padding = normalized.length % 4 === 0 ? '' : '='.repeat(4 - (normalized.length % 4));
      const decoded = atob(normalized + padding);
      return JSON.parse(decoded) as Record<string, unknown>;
    } catch {
      return null;
    }
  }

  private clearLocalState(expired: boolean): void {
    localStorage.removeItem('username');
    localStorage.removeItem('accessToken');
    localStorage.removeItem('sessionId');
    localStorage.removeItem('userId');
    localStorage.removeItem(ID_TOKEN_STORAGE_KEY);
    localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY);
    this.isLogin = false;
    this.userName = null;
    this.id = null;
    this.sessionId = null;
    this.isAdmin = false;
    this.sessionExpired.set(expired);
  }

  updateUserLoginState(): void {
    const username = localStorage.getItem('username');
    const token = localStorage.getItem('accessToken');
    const sessionId = localStorage.getItem('sessionId');
    if (token && username) {
      this.userName = username;
      this.isLogin = true;
      this.id = localStorage.getItem('userId');
      this.sessionId = sessionId;
      this.isAdmin = this.hasAdminRole(this.getRoles(token));
    } else {
      this.isLogin = false;
      this.userName = null;
      this.id = null;
      this.sessionId = null;
      this.isAdmin = false;
    }
  }

  getRoles(token = this.getAccessToken() ?? ''): string[] {
    return this.extractJwtClaims(token, 'role');
  }

  getOidcClientId(): string {
    return OIDC_CLIENT_ID;
  }

  private hasAdminRole(roles: string[]): boolean {
    return roles.includes(SUPER_ADMIN_ROLE) || roles.includes(ADMIN_USER_ROLE);
  }

  isAuthenticated(): boolean {
    return this.isLogin;
  }

  peekReturnUrl(): string | null {
    return sessionStorage.getItem(RETURN_URL_STORAGE_KEY);
  }

  consumeReturnUrl(defaultUrl = '/user/list'): string {
    const returnUrl = sessionStorage.getItem(RETURN_URL_STORAGE_KEY);
    sessionStorage.removeItem(RETURN_URL_STORAGE_KEY);
    return returnUrl?.trim() || defaultUrl;
  }

  async startLogin(returnUrl?: string | null): Promise<void> {
    sessionStorage.setItem(RETURN_URL_STORAGE_KEY, returnUrl?.trim() || '/user/list');
    this.sessionExpired.set(false);
    const verifier = this.createRandomString(48);
    const state = this.createRandomString(24);
    const codeChallenge = await this.buildPkceChallenge(verifier);

    sessionStorage.setItem(CODE_VERIFIER_STORAGE_KEY, verifier);
    sessionStorage.setItem(OIDC_STATE_STORAGE_KEY, state);

    window.location.assign(this.buildAuthorizeUrl(codeChallenge, state));
  }

  async completeLogin(code: string, state: string | null): Promise<void> {
    const expectedState = sessionStorage.getItem(OIDC_STATE_STORAGE_KEY);
    const verifier = sessionStorage.getItem(CODE_VERIFIER_STORAGE_KEY);

    if (!code || !state || !expectedState || state !== expectedState || !verifier) {
      throw new Error('Invalid authorization response');
    }

    const body = new URLSearchParams({
      grant_type: 'authorization_code',
      client_id: OIDC_CLIENT_ID,
      code,
      redirect_uri: this.getRedirectUrl(),
      code_verifier: verifier,
    });

    const response = await firstValueFrom(
      this.http.post<TokenResponseDto>(`${this.getAuthorityUrl()}/connect/token`, body.toString(), {
        headers: new HttpHeaders({
          'Content-Type': 'application/x-www-form-urlencoded',
        }),
      }),
    );

    if (!response?.access_token) {
      throw new Error('Token exchange failed');
    }

    const userName = this.extractUserName(response.access_token) ?? 'admin';
    const sessionId = this.extractJwtClaim(response.access_token, 'sid');
    const userId = this.extractJwtClaim(response.access_token, 'sub');

    this.saveLoginState(userName, response.access_token, sessionId);
    this.id = userId;
    if (userId) {
      localStorage.setItem('userId', userId);
    }

    if (response.refresh_token) {
      localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, response.refresh_token);
    }
    if (response.id_token) {
      localStorage.setItem(ID_TOKEN_STORAGE_KEY, response.id_token);
    }

    sessionStorage.removeItem(CODE_VERIFIER_STORAGE_KEY);
    sessionStorage.removeItem(OIDC_STATE_STORAGE_KEY);
  }

  private extractUserName(token: string): string | null {
    return this.pickUserName({
      preferred_username: this.extractJwtClaim(token, 'preferred_username'),
      name: this.extractJwtClaim(token, 'name'),
      email: this.extractJwtClaim(token, 'email'),
      sub: this.extractJwtClaim(token, 'sub'),
    });
  }

  user(): { preferred_username?: string; name?: string } | null {
    if (this.isLogin && this.userName) {
      return {
        preferred_username: this.userName,
        name: this.userName
      };
    }
    return null;
  }

  logout(): void {
    this.clearLocalState(true);
  }

  getSessionId(): string | null {
    return this.sessionId ?? localStorage.getItem('sessionId');
  }

  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  handleUnauthorized(): void {
    this.clearLocalState(true);
  }

  logoutFromServer(): void {
    const idToken = localStorage.getItem(ID_TOKEN_STORAGE_KEY);
    const logoutUrl = new URL(`${this.getAuthorityUrl()}/connect/logout`);
    logoutUrl.searchParams.set('post_logout_redirect_uri', window.location.origin);
    if (idToken) {
      logoutUrl.searchParams.set('id_token_hint', idToken);
    }

    this.clearLocalState(false);
    window.location.assign(logoutUrl.toString());
  }
}
