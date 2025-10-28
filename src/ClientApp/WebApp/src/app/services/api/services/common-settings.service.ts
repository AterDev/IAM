import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/ater/page-list.model';
import { SystemSettingItemDto } from '../models/common-mod/system-setting-item-dto.model';
import { SystemSettingAddDto } from '../models/common-mod/system-setting-add-dto.model';
import { SystemSettingDetailDto } from '../models/common-mod/system-setting-detail-dto.model';
import { SystemSettingUpdateDto } from '../models/common-mod/system-setting-update-dto.model';
/**
 * Common settings controller
 */
@Injectable({ providedIn: 'root' })
export class CommonSettingsService extends BaseService {
  /**
   * Get paged system settings
   * @param key Filter by key (partial match)
   * @param category Filter by category
   * @param isPublic Filter by public flag
   * @param isEditable Filter by editable flag
   * @param pageIndex number
   * @param pageSize number
   * @param orderBy Record<string, boolean>
   */
  getSettings(key: string | null, category: string | null, isPublic: boolean | null, isEditable: boolean | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<PageList<SystemSettingItemDto>> {
    const _url = `/api/CommonSettings?key=${key ?? ''}&category=${category ?? ''}&isPublic=${isPublic ?? ''}&isEditable=${isEditable ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<SystemSettingItemDto>>('get', _url);
  }
  /**
   * Create new system setting
   * @param data SystemSettingAddDto
   */
  createSetting(data: SystemSettingAddDto): Observable<SystemSettingDetailDto> {
    const _url = `/api/CommonSettings`;
    return this.request<SystemSettingDetailDto>('post', _url, data);
  }
  /**
   * Get public settings (no authentication required)
   */
  getPublicSettings(): Observable<SystemSettingItemDto[]> {
    const _url = `/api/CommonSettings/public`;
    return this.request<SystemSettingItemDto[]>('get', _url);
  }
  /**
   * Get system setting detail by id
   * @param id Setting id
   */
  getDetail(id: string): Observable<SystemSettingDetailDto> {
    const _url = `/api/CommonSettings/${id}`;
    return this.request<SystemSettingDetailDto>('get', _url);
  }
  /**
   * Update system setting
   * @param id Setting id
   * @param data SystemSettingUpdateDto
   */
  updateSetting(id: string, data: SystemSettingUpdateDto): Observable<SystemSettingDetailDto> {
    const _url = `/api/CommonSettings/${id}`;
    return this.request<SystemSettingDetailDto>('put', _url, data);
  }
  /**
   * Delete system setting
   * @param id Setting id
   */
  deleteSetting(id: string): Observable<any> {
    const _url = `/api/CommonSettings/${id}`;
    return this.request<any>('delete', _url);
  }
  /**
   * Get system setting by key
   * @param key Setting key
   */
  getByKey(key: string): Observable<SystemSettingDetailDto> {
    const _url = `/api/CommonSettings/key/${key}`;
    return this.request<SystemSettingDetailDto>('get', _url);
  }
}