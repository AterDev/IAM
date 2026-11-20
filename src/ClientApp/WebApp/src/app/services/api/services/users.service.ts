import { BaseService } from '../base.service';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageList } from '../models/perigon/page-list.model';
import { UserItemDto } from '../models/iammod/user-item-dto.model';
import { UserAddDto } from '../models/iammod/user-add-dto.model';
import { UserDetailDto } from '../models/iammod/user-detail-dto.model';
import { UserUpdateDto } from '../models/iammod/user-update-dto.model';
/**
 *
 */
@Injectable({ providedIn: 'root' })
export class UsersService extends BaseService {
  /**
   * getUsers
   * @param userName Filter by username
   * @param email Filter by email
   * @param phoneNumber Filter by phone number
   * @param lockoutEnabled Filter by lockout status
   * @param startDate Filter by date range start
   * @param endDate Filter by date range end
   * @param pageIndex any
   * @param pageSize any
   * @param orderBy string
   */
  getUsers(userName: string | null, email: string | null, phoneNumber: string | null, lockoutEnabled: boolean | null, startDate: Date | null, endDate: Date | null, pageIndex: any | null, pageSize: any | null, orderBy: string | null): Observable<PageList<UserItemDto>> {
    const _url = `/api/Users?UserName=${userName ?? ''}&Email=${email ?? ''}&PhoneNumber=${phoneNumber ?? ''}&LockoutEnabled=${lockoutEnabled ?? ''}&StartDate=${startDate ?? ''}&EndDate=${endDate ?? ''}&pageIndex=${pageIndex ?? ''}&pageSize=${pageSize ?? ''}&orderBy=${orderBy ?? ''}`;
    return this.request<PageList<UserItemDto>>('get', _url);
  }
  /**
   * createUser
   * @param data UserAddDto
   */
  createUser(data: UserAddDto): Observable<UserDetailDto> {
    const _url = `/api/Users`;
    return this.request<UserDetailDto>('post', _url, data);
  }
  /**
   * getByUserName
   * @param username string
   */
  getByUserName(username: string): Observable<UserDetailDto> {
    const _url = `/api/Users/username/${username}`;
    return this.request<UserDetailDto>('get', _url);
  }
  /**
   * getDetail
   * @param id string
   */
  getDetail(id: string): Observable<UserDetailDto> {
    const _url = `/api/Users/${id}`;
    return this.request<UserDetailDto>('get', _url);
  }
  /**
   * updateUser
   * @param id string
   * @param data UserUpdateDto
   */
  updateUser(id: string, data: UserUpdateDto): Observable<UserDetailDto> {
    const _url = `/api/Users/${id}`;
    return this.request<UserDetailDto>('put', _url, data);
  }
  /**
   * deleteUser
   * @param id string
   * @param hardDelete boolean
   */
  deleteUser(id: string, hardDelete: boolean): Observable<boolean> {
    const _url = `/api/Users/${id}?hardDelete=${hardDelete ?? ''}`;
    return this.request<boolean>('delete', _url);
  }
  /**
   * updateStatus
   * @param id string
   * @param data any
   */
  updateStatus(id: string, data: any): Observable<boolean> {
    const _url = `/api/Users/${id}/status`;
    return this.request<boolean>('patch', _url, data);
  }
  /**
   * changePassword
   * @param id string
   * @param data string
   */
  changePassword(id: string, data: string): Observable<boolean> {
    const _url = `/api/Users/${id}/password`;
    return this.request<boolean>('post', _url, data);
  }
  /**
   * assignRoles
   * @param id string
   * @param data string[]
   */
  assignRoles(id: string, data: string[]): Observable<boolean> {
    const _url = `/api/Users/${id}/roles`;
    return this.request<boolean>('post', _url, data);
  }
}