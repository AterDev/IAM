import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
/**
 * CommonController
 */
@Injectable({ providedIn: 'root' })
export class CommonService extends BaseService {
  /**
   * get enum dictionary
   */
  getEnumDictionary(): Observable<Record<string, any[]>> {
    const _url = `/api/Common/enums`;
    return this.request<Record<string, any[]>>('get', _url);
  }
}