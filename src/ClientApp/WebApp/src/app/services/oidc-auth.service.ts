import { Injectable, signal, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ApiClient } from './api/api-client';
import { TokenResponseDto } from './api/models/identity-mod/token-response-dto.model';

/**
 * PKCE helper utilities
 */
class PkceHelper {
  /**
   * Generate a random code verifier
   */
  static generateCodeVerifier(): string {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    return this.base64UrlEncode(array);
  }

  /**
   * Generate code challenge from verifier using SHA-256
   */
  static async generateCodeChallenge(verifier: string): Promise<string> {
    const encoder = new TextEncoder();
    const data = encoder.encode(verifier);
    const hash = await crypto.subtle.digest('SHA-256', data);
    return this.base64UrlEncode(new Uint8Array(hash));
  }

  /**
   * Base64 URL encode
   */
  private static base64UrlEncode(buffer: Uint8Array): string {
    const base64 = btoa(String.fromCharCode(...buffer));
    return base64
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');
  }

  /**
   * Generate random state parameter
   */
  static generateState(): string {
    const array = new Uint8Array(16);
    crypto.getRandomValues(array);
    return this.base64UrlEncode(array);
  }
}

/**
 * User information from token
 */
export interface UserInfo {
  sub: string;
  name?: string;
  email?: string;
  preferred_username?: string;
  roles?: string[];
}

/**
 * Token response from OAuth server
 */
export interface TokenResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  refresh_token?: string;
  id_token?: string;
  scope?: string;
}

/**
 * Authentication state
 */
interface AuthState {
  isAuthenticated: boolean;
  user: UserInfo | null;
  accessToken: string | null;
  refreshToken: string | null;
  idToken: string | null;
  expiresAt: number | null;
}

/**
 * OIDC/OAuth2 Authentication Service with PKCE support
 * Uses Angular Signals for reactive state management
 */
@Injectable({
  providedIn: 'root'
})
export class OidcAuthService {
  private router = inject(Router);
  private apiClient = inject(ApiClient);

  // Signal-based state
  private authState = signal<AuthState>({
    isAuthenticated: false,
    user: null,
    accessToken: null,
    refreshToken: null,
    idToken: null,
    expiresAt: null
  });

  // Public computed signals
  readonly isAuthenticated = computed(() => this.authState().isAuthenticated);
  readonly user = computed(() => this.authState().user);
  readonly accessToken = computed(() => this.authState().accessToken);

  constructor() {
    this.loadStateFromStorage();
    this.scheduleTokenRefresh();
  }

  /**
   * Start authorization code flow with PKCE
   */
  async startAuthorizationCodeFlow(
    clientId: string,
    redirectUri: string,
    scope: string = 'openid profile email'
  ): Promise<void> {
    const codeVerifier = PkceHelper.generateCodeVerifier();
    const codeChallenge = await PkceHelper.generateCodeChallenge(codeVerifier);
    const state = PkceHelper.generateState();

    // Store PKCE parameters
    sessionStorage.setItem('pkce_code_verifier', codeVerifier);
    sessionStorage.setItem('pkce_state', state);
    sessionStorage.setItem('pkce_redirect_uri', redirectUri);

    // Build authorization URL
    const params = new URLSearchParams({
      response_type: 'code',
      client_id: clientId,
      redirect_uri: redirectUri,
      scope: scope,
      state: state,
      code_challenge: codeChallenge,
      code_challenge_method: 'S256'
    });

    // Redirect to authorization endpoint
    window.location.href = `/connect/authorize?${params.toString()}`;
  }

  /**
   * Handle authorization callback
   */
  async handleCallback(
    code: string,
    state: string,
    clientId: string
  ): Promise<boolean> {
    // Validate state
    const savedState = sessionStorage.getItem('pkce_state');
    if (!savedState || savedState !== state) {
      console.error('Invalid state parameter');
      return false;
    }

    // Get PKCE parameters
    const codeVerifier = sessionStorage.getItem('pkce_code_verifier');
    const redirectUri = sessionStorage.getItem('pkce_redirect_uri');

    if (!codeVerifier || !redirectUri) {
      console.error('Missing PKCE parameters');
      return false;
    }

    try {
      // Exchange code for tokens
      const tokenResponse = await this.exchangeCodeForTokens(
        code,
        clientId,
        redirectUri,
        codeVerifier
      );

      // Save tokens
      this.setTokens(tokenResponse);

      // Clean up session storage
      sessionStorage.removeItem('pkce_code_verifier');
      sessionStorage.removeItem('pkce_state');
      sessionStorage.removeItem('pkce_redirect_uri');

      return true;
    } catch (error) {
      console.error('Token exchange failed', error);
      return false;
    }
  }

  /**
   * Exchange authorization code for tokens
   */
  private async exchangeCodeForTokens(
    code: string,
    clientId: string,
    redirectUri: string,
    codeVerifier: string
  ): Promise<TokenResponseDto> {
    const tokenData = {
      grant_type: 'authorization_code',
      code: code,
      client_id: clientId,
      redirect_uri: redirectUri,
      code_verifier: codeVerifier
    };

    return new Promise((resolve, reject) => {
      this.apiClient.oAuth.token(tokenData).subscribe({
        next: (response) => resolve(response),
        error: (error) => reject(error)
      });
    });
  }

  /**
   * Login with username and password (Resource Owner Password Credentials flow)
   */
  async loginWithPassword(
    username: string,
    password: string,
    clientId: string,
    scope: string = 'openid profile email'
  ): Promise<boolean> {
    try {
      const tokenData = {
        grant_type: 'password',
        username: username,
        password: password,
        client_id: clientId,
        scope: scope
      };

      const tokenResponse = await new Promise<TokenResponseDto>((resolve, reject) => {
        this.apiClient.oAuth.token(tokenData).subscribe({
          next: (response) => resolve(response),
          error: (error) => reject(error)
        });
      });

      this.setTokens(tokenResponse);
      return true;
    } catch (error) {
      console.error('Login failed', error);
      return false;
    }
  }

  /**
   * Refresh access token
   */
  async refreshAccessToken(): Promise<boolean> {
    const currentRefreshToken = this.authState().refreshToken;

    if (!currentRefreshToken) {
      console.error('No refresh token available');
      return false;
    }

    try {
      const tokenData = {
        grant_type: 'refresh_token',
        refresh_token: currentRefreshToken
      };

      const tokenResponse = await new Promise<TokenResponseDto>((resolve, reject) => {
        this.apiClient.oAuth.token(tokenData).subscribe({
          next: (response) => resolve(response),
          error: (error) => reject(error)
        });
      });

      this.setTokens(tokenResponse);
      return true;
    } catch (error) {
      console.error('Token refresh failed', error);
      this.logout();
      return false;
    }
  }

  /**
   * Logout
   */
  logout(): void {
    // Clear tokens
    this.authState.set({
      isAuthenticated: false,
      user: null,
      accessToken: null,
      refreshToken: null,
      idToken: null,
      expiresAt: null
    });

    // Clear storage
    localStorage.removeItem('auth_state');
    sessionStorage.clear();

    // Navigate to login
    this.router.navigate(['/login']);
  }

  /**
   * Set tokens from response
   */
  private setTokens(tokenResponse: TokenResponseDto): void {
    const expiresAt = Date.now() + (tokenResponse.expiresIn ?? 1 * 1000);
    const user = this.parseIdToken(tokenResponse.idToken!);

    const newState: AuthState = {
      isAuthenticated: true,
      user: user,
      accessToken: tokenResponse.accessToken!,
      refreshToken: tokenResponse.refreshToken || null,
      idToken: tokenResponse.idToken || null,
      expiresAt: expiresAt
    };

    this.authState.set(newState);
    this.saveStateToStorage(newState);
    this.scheduleTokenRefresh();
  }

  /**
   * Parse ID token (JWT)
   */
  private parseIdToken(idToken?: string): UserInfo | null {
    if (!idToken) {
      return null;
    }

    try {
      const parts = idToken.split('.');
      if (parts.length !== 3) {
        return null;
      }

      const payload = JSON.parse(atob(parts[1]));
      return {
        sub: payload.sub,
        name: payload.name,
        email: payload.email,
        preferred_username: payload.preferred_username,
        roles: payload.roles || payload.role
      };
    } catch (error) {
      console.error('Failed to parse ID token', error);
      return null;
    }
  }

  /**
   * Save state to storage
   */
  private saveStateToStorage(state: AuthState): void {
    try {
      localStorage.setItem('auth_state', JSON.stringify(state));
    } catch (error) {
      console.error('Failed to save auth state', error);
    }
  }

  /**
   * Load state from storage
   */
  private loadStateFromStorage(): void {
    try {
      const saved = localStorage.getItem('auth_state');
      if (!saved) {
        return;
      }

      const state = JSON.parse(saved) as AuthState;

      // Check if token is expired
      if (state.expiresAt && state.expiresAt < Date.now()) {
        // Try to refresh
        if (state.refreshToken) {
          this.authState.set(state);
          this.refreshAccessToken();
        } else {
          this.logout();
        }
      } else {
        this.authState.set(state);
      }
    } catch (error) {
      console.error('Failed to load auth state', error);
    }
  }

  /**
   * Schedule token refresh
   */
  private scheduleTokenRefresh(): void {
    const state = this.authState();
    if (!state.expiresAt || !state.refreshToken) {
      return;
    }

    // Refresh 5 minutes before expiry
    const refreshTime = state.expiresAt - Date.now() - (5 * 60 * 1000);

    if (refreshTime > 0) {
      setTimeout(() => {
        this.refreshAccessToken();
      }, refreshTime);
    }
  }

  /**
   * Check if token needs refresh
   */
  needsRefresh(): boolean {
    const state = this.authState();
    if (!state.expiresAt) {
      return false;
    }

    // Refresh if less than 5 minutes remaining
    const timeRemaining = state.expiresAt - Date.now();
    return timeRemaining < (5 * 60 * 1000);
  }

  /**
   * Get current access token (for HTTP interceptor)
   */
  getAccessToken(): string | null {
    return this.authState().accessToken;
  }
}
