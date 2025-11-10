import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { OidcConfigurationDto } from '../models/identity-mod/oidc-configuration-dto.model';
import { JwksDto } from '../models/identity-mod/jwks-dto.model';
import { UserInfoDto } from '../models/identity-mod/user-info-dto.model';
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
  /**
   * UserInfo endpoint (OIDC)
   */
  getUserInfo(): Observable<UserInfoDto> {
    const _url = `/connect/userinfo`;
    return this.request<UserInfoDto>('get', _url);
  }
  /**
   * UserInfo endpoint (OIDC)
   */
  getUserInfoPOST(): Observable<UserInfoDto> {
    const _url = `/connect/userinfo`;
    return this.request<UserInfoDto>('post', _url);
  }
}