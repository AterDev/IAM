import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseService } from './api/base.service';

export interface OAuthInteractionScope {
  name: string;
  displayName: string;
  description?: string | null;
  required: boolean;
}

export interface AuthorizeInteractionContext {
  clientId: string;
  clientName: string;
  clientDescription?: string | null;
  scope?: string | null;
  requestedScopes: OAuthInteractionScope[];
  redirectUri: string;
  responseType: string;
  state?: string | null;
  nonce?: string | null;
  codeChallenge?: string | null;
  codeChallengeMethod?: string | null;
  responseMode?: string | null;
  userName?: string | null;
  hasValidConsent: boolean;
}

export interface AuthorizeInteractionDecisionRequest {
  clientId: string;
  redirectUri: string;
  responseType: string;
  scope?: string | null;
  state?: string | null;
  nonce?: string | null;
  codeChallenge?: string | null;
  codeChallengeMethod?: string | null;
  responseMode?: string | null;
  approve: boolean;
  rememberConsent: boolean;
}

export interface AuthorizeInteractionDecisionResponse {
  status: string;
  redirectUrl: string;
  message?: string | null;
}

export interface DeviceAuthorizationInteraction {
  userCode: string;
  status: 'pending' | 'approved' | 'denied' | 'expired' | 'invalid';
  message?: string | null;
  clientId?: string | null;
  clientName?: string | null;
  clientDescription?: string | null;
  scope?: string | null;
  requestedScopes: OAuthInteractionScope[];
  expiresAt?: string | null;
  canApprove: boolean;
  canDeny: boolean;
}

export interface DeviceAuthorizationDecisionRequest {
  userCode: string;
  approve: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class OauthInteractionService extends BaseService {
  getAuthorizeInteraction(params: Record<string, string>): Observable<AuthorizeInteractionContext> {
    return this.request<AuthorizeInteractionContext>('get', `/connect/interaction/authorize${this.buildQuery(params)}`);
  }

  submitAuthorizeDecision(data: AuthorizeInteractionDecisionRequest): Observable<AuthorizeInteractionDecisionResponse> {
    return this.request<AuthorizeInteractionDecisionResponse>('post', '/connect/interaction/authorize/decision', data);
  }

  getDeviceInteraction(userCode: string): Observable<DeviceAuthorizationInteraction> {
    return this.request<DeviceAuthorizationInteraction>('get', `/connect/interaction/device${this.buildQuery({ userCode })}`);
  }

  submitDeviceDecision(data: DeviceAuthorizationDecisionRequest): Observable<DeviceAuthorizationInteraction> {
    return this.request<DeviceAuthorizationInteraction>('post', '/connect/interaction/device/decision', data);
  }

  private buildQuery(params: Record<string, string | null | undefined>): string {
    const search = new URLSearchParams();

    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        search.set(key, value);
      }
    });

    const query = search.toString();
    return query ? `?${query}` : '';
  }
}
