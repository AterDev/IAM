import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthorizeInteractionContextDto } from '../models/iammod/authorize-interaction-context-dto.model';
import { AuthorizeInteractionDecisionDto } from '../models/iammod/authorize-interaction-decision-dto.model';
import { AuthorizeInteractionDecisionResponseDto } from '../models/iammod/authorize-interaction-decision-response-dto.model';
import { DeviceAuthorizationInteractionDto } from '../models/iammod/device-authorization-interaction-dto.model';
import { DeviceAuthorizationDecisionDto } from '../models/iammod/device-authorization-decision-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class OAuthInteractionService extends BaseService {
  /**
   * oAuthInteractionGetAuthorizeInteraction
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
  oAuthInteractionGetAuthorizeInteraction(responseType: string, clientId: string, redirectUri: string, scope: string | null, state: string | null, codeChallenge: string | null, codeChallengeMethod: string | null, responseMode: string | null, nonce: string | null, prompt: string | null): Observable<AuthorizeInteractionContextDto> {
    const _url = `/connect/interaction/authorize?ResponseType=${responseType ?? ''}&ClientId=${clientId ?? ''}&RedirectUri=${redirectUri ?? ''}&Scope=${scope ?? ''}&State=${state ?? ''}&CodeChallenge=${codeChallenge ?? ''}&CodeChallengeMethod=${codeChallengeMethod ?? ''}&ResponseMode=${responseMode ?? ''}&Nonce=${nonce ?? ''}&Prompt=${prompt ?? ''}`;
    return this.request<AuthorizeInteractionContextDto>('get', _url);
  }
  /**
   * oAuthInteractionSubmitAuthorizeDecision
   * @param data AuthorizeInteractionDecisionDto
   */
  oAuthInteractionSubmitAuthorizeDecision(data: AuthorizeInteractionDecisionDto): Observable<AuthorizeInteractionDecisionResponseDto> {
    const _url = `/connect/interaction/authorize/decision`;
    return this.request<AuthorizeInteractionDecisionResponseDto>('post', _url, data);
  }
  /**
   * oAuthInteractionGetDeviceInteraction
   * @param userCode string
   */
  oAuthInteractionGetDeviceInteraction(userCode: string): Observable<DeviceAuthorizationInteractionDto> {
    const _url = `/connect/interaction/device?userCode=${userCode ?? ''}`;
    return this.request<DeviceAuthorizationInteractionDto>('get', _url);
  }
  /**
   * oAuthInteractionSubmitDeviceDecision
   * @param data DeviceAuthorizationDecisionDto
   */
  oAuthInteractionSubmitDeviceDecision(data: DeviceAuthorizationDecisionDto): Observable<DeviceAuthorizationInteractionDto> {
    const _url = `/connect/interaction/device/decision`;
    return this.request<DeviceAuthorizationInteractionDto>('post', _url, data);
  }
}