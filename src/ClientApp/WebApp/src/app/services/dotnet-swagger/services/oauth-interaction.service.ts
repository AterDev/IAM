import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthorizeInteractionContextDto } from '../models/iammod/authorize-interaction-context-dto.model';
import { AuthorizeInteractionDecisionDto } from '../models/iammod/authorize-interaction-decision-dto.model';
import { AuthorizeInteractionDecisionResponseDto } from '../models/iammod/authorize-interaction-decision-response-dto.model';
import { DeviceAuthorizationInteractionDto } from '../models/iammod/device-authorization-interaction-dto.model';
import { DeviceAuthorizationDecisionDto } from '../models/iammod/device-authorization-decision-dto.model';
/**
 * Interaction endpoints used by the SPA authorize and device-code pages.
 */
@Injectable({ providedIn: 'root' })
export class OAuthInteractionService extends BaseService {
  /**
   * Get interaction context for the SPA authorize page.
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
  getAuthorizeInteraction(response_type: string, client_id: string, redirect_uri: string, scope: string | null, state: string | null, code_challenge: string | null, code_challenge_method: string | null, response_mode: string | null, nonce: string | null, prompt: string | null): Observable<AuthorizeInteractionContextDto> {
    const _url = `/connect/interaction/authorize?response_type=${response_type ?? ''}&client_id=${client_id ?? ''}&redirect_uri=${redirect_uri ?? ''}&scope=${scope ?? ''}&state=${state ?? ''}&code_challenge=${code_challenge ?? ''}&code_challenge_method=${code_challenge_method ?? ''}&response_mode=${response_mode ?? ''}&nonce=${nonce ?? ''}&prompt=${prompt ?? ''}`;
    return this.request<AuthorizeInteractionContextDto>('get', _url);
  }
  /**
   * Submit an allow or deny decision for the SPA authorize page.
   * @param data AuthorizeInteractionDecisionDto
   */
  submitAuthorizeDecision(data: AuthorizeInteractionDecisionDto): Observable<AuthorizeInteractionDecisionResponseDto> {
    const _url = `/connect/interaction/authorize/decision`;
    return this.request<AuthorizeInteractionDecisionResponseDto>('post', _url, data);
  }
  /**
   * Get device-code interaction context by user code.
   * @param userCode string
   */
  getDeviceInteraction(userCode: string | null): Observable<DeviceAuthorizationInteractionDto> {
    const _url = `/connect/interaction/device?userCode=${userCode ?? ''}`;
    return this.request<DeviceAuthorizationInteractionDto>('get', _url);
  }
  /**
   * Submit an allow or deny decision for a device-code interaction.
   * @param data DeviceAuthorizationDecisionDto
   */
  submitDeviceDecision(data: DeviceAuthorizationDecisionDto): Observable<DeviceAuthorizationInteractionDto> {
    const _url = `/connect/interaction/device/decision`;
    return this.request<DeviceAuthorizationInteractionDto>('post', _url, data);
  }
}