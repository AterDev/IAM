import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { OidcConfigurationDto } from '../models/iammod/oidc-configuration-dto.model';
import { JwksDto } from '../models/iammod/jwks-dto.model';
/**
 * OpenID Connect Discovery endpoint controller
 */
@Injectable({ providedIn: 'root' })
export class DiscoveryService extends BaseService {
  /**
   * OpenID Connect Discovery document
   */
  getConfiguration(): Observable<OidcConfigurationDto> {
    const _url = `/.well-known/openid-configuration`;
    return this.request<OidcConfigurationDto>('get', _url);
  }
  /**
   * JSON Web Key Set (JWKS) endpoint
   */
  getJwks(): Observable<JwksDto> {
    const _url = `/.well-known/jwks`;
    return this.request<JwksDto>('get', _url);
  }
}