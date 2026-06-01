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
  getAuthorizeInteraction(responseType: string, clientId: string, redirectUri: string, scope: string | null, state: string | null, codeChallenge: string | null, codeChallengeMethod: string | null, responseMode: string | null, nonce: string | null, prompt: string | null): Observable<AuthorizeInteractionContextDto> {
    const _url = `/connect/interaction/authorize?response_type=${responseType ?? ''}&client_id=${clientId ?? ''}&redirect_uri=${redirectUri ?? ''}&scope=${scope ?? ''}&state=${state ?? ''}&code_challenge=${codeChallenge ?? ''}&code_challenge_method=${codeChallengeMethod ?? ''}&response_mode=${responseMode ?? ''}&nonce=${nonce ?? ''}&prompt=${prompt ?? ''}`;
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