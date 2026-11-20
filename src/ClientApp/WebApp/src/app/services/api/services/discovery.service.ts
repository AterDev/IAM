import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class DiscoveryService extends BaseService {
  /**
   * getConfiguration
   */
  getConfiguration(): Observable<void> {
    const _url = `/.well-known/openid-configuration`;
    return this.request<void>('get', _url);
  }
  /**
   * getJwks
   */
  getJwks(): Observable<void> {
    const _url = `/.well-known/jwks`;
    return this.request<void>('get', _url);
  }
}