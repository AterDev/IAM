import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/ater/page-list.model';
import { UserItemDto } from '../models/identity-mod/user-item-dto.model';
import { UserAddDto } from '../models/identity-mod/user-add-dto.model';
import { UserDetailDto } from '../models/identity-mod/user-detail-dto.model';
import { UserUpdateDto } from '../models/identity-mod/user-update-dto.model';
/**
 * User management controller
 */
@Injectable({ providedIn: 'root' })
export class UsersService extends BaseService {
  /**
   * Get paged users
   * @param userName Filter by username
   * @param email Filter by email
   * @param phoneNumber Filter by phone number
   * @param lockoutEnabled Filter by lockout status
   * @param startDate Filter by date range start
   * @param endDate Filter by date range end
   * @param pageIndex number
   * @param pageSize number
   * @param orderBy Record<string, boolean>
   */
  getUsers(userName: string | null, email: string | null, phoneNumber: string | null, lockoutEnabled: boolean | null, startDate: Date | null, endDate: Date | null, pageIndex: number | null, pageSize: number | null, orderBy: Record<string, boolean> | null): Observable<PageList<UserItemDto>> {
    const _url = `/api/Users?userName=${userName ?? ''}&email=${email ?? ''}&phoneNumber=${phoneNumber ?? ''}&lockoutEnabled=${lockoutEnabled ?? ''}&startDate=${startDate ?? ''}&endDate=${endDate ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<UserItemDto>>('get', _url);
  }
  /**
   * Create new user
   * @param data UserAddDto
   */
  createUser(data: UserAddDto): Observable<UserDetailDto> {
    const _url = `/api/Users`;
    return this.request<UserDetailDto>('post', _url, data);
  }
  /**
   * Get user detail by id
   * @param id User unique identifier
   */
  getDetail(id: string): Observable<UserDetailDto> {
    const _url = `/api/Users/${id}`;
    return this.request<UserDetailDto>('get', _url);
  }
  /**
   * Update user
   * @param id User unique identifier
   * @param data UserUpdateDto
   */
  updateUser(id: string, data: UserUpdateDto): Observable<UserDetailDto> {
    const _url = `/api/Users/${id}`;
    return this.request<UserDetailDto>('put', _url, data);
  }
  /**
   * Delete user
   * @param id User id
   * @param hardDelete Perform hard delete (default false)
   */
  deleteUser(id: string, hardDelete: boolean | null): Observable<any> {
    const _url = `/api/Users/${id}?hardDelete=${hardDelete ?? ''}`;
    return this.request<any>('delete', _url);
  }
  /**
   * Get user by username
   * @param username Username
   */
  getByUserName(username: string): Observable<UserDetailDto> {
    const _url = `/api/Users/username/${username}`;
    return this.request<UserDetailDto>('get', _url);
  }
  /**
   * Update user status (lock/unlock)
   * @param id User id
   * @param data Date
   */
  updateStatus(id: string, data: Date): Observable<any> {
    const _url = `/api/Users/${id}/status`;
    return this.request<any>('patch', _url, data);
  }
  /**
   * Change user password
   * @param id User id
   * @param data string
   */
  changePassword(id: string, data: string): Observable<any> {
    const _url = `/api/Users/${id}/password`;
    return this.request<any>('post', _url, data);
  }
  /**
   * Assign roles to user
   * @param id User id
   * @param data string[]
   */
  assignRoles(id: string, data: string[]): Observable<any> {
    const _url = `/api/Users/${id}/roles`;
    return this.request<any>('post', _url, data);
  }
}