import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseService } from './api/base.service';
import {
  ClientPermissionSyncDto,
  PermissionFilter,
  PermissionItem,
  PermissionPage,
  PermissionTreeNode,
  PermissionUpsertDto,
} from './permission-admin.models';

@Injectable({
  providedIn: 'root',
})
export class PermissionAdminService extends BaseService {
  getPermissions(filter: PermissionFilter): Observable<PermissionPage> {
    return this.request<PermissionPage>('get', `/api/Permissions${this.toQueryString(filter)}`);
  }

  getPermissionTree(filter: PermissionFilter): Observable<PermissionTreeNode[]> {
    return this.request<PermissionTreeNode[]>('get', `/api/Permissions/tree${this.toQueryString(filter)}`);
  }

  getPermissionDetail(id: string): Observable<PermissionItem> {
    return this.request<PermissionItem>('get', `/api/Permissions/${id}`);
  }

  createPermission(data: PermissionUpsertDto): Observable<PermissionItem> {
    return this.request<PermissionItem>('post', '/api/Permissions', data);
  }

  updatePermission(id: string, data: PermissionUpsertDto): Observable<PermissionItem> {
    return this.request<PermissionItem>('put', `/api/Permissions/${id}`, data);
  }

  deletePermission(id: string): Observable<void> {
    return this.request<void>('delete', `/api/Permissions/${id}`);
  }

  getRolePermissionCodes(roleId: string): Observable<string[]> {
    return this.request<string[]>('get', `/api/Roles/${roleId}/permissions`);
  }

  getRolePermissionTree(roleId: string, filter: PermissionFilter): Observable<PermissionTreeNode[]> {
    return this.request<PermissionTreeNode[]>('get', `/api/Roles/${roleId}/permission-tree${this.toQueryString(filter)}`);
  }

  grantRolePermissions(roleId: string, permissionCodes: string[]): Observable<void> {
    return this.request<void>('post', `/api/Roles/${roleId}/permissions`, { permissionCodes });
  }

  getClientPermissionCodes(clientId: string): Observable<string[]> {
    return this.request<string[]>('get', `/api/Clients/${clientId}/permissions`);
  }

  getClientPermissionTree(clientId: string, filter: PermissionFilter): Observable<PermissionTreeNode[]> {
    return this.request<PermissionTreeNode[]>('get', `/api/Clients/${clientId}/permission-tree${this.toQueryString(filter)}`);
  }

  assignClientPermissions(clientId: string, permissionCodes: string[]): Observable<void> {
    return this.request<void>('post', `/api/Clients/${clientId}/permissions`, permissionCodes);
  }

  syncClientMenuPermissions(clientId: string, data: ClientPermissionSyncDto): Observable<void> {
    return this.request<void>('post', `/api/Clients/${clientId}/menu-permissions:sync`, data);
  }

  getMyMenuTree(clientCode: string): Observable<PermissionTreeNode[]> {
    return this.request<PermissionTreeNode[]>('get', `/api/Permissions/my-menu-tree${this.toQueryString({ clientCode })}`);
  }

  private toQueryString(filter: object): string {
    const params = new URLSearchParams();

    Object.entries(filter as Record<string, unknown>).forEach(([key, value]) => {
      if (value === undefined || value === null || value === '') {
        return;
      }

      if (Array.isArray(value)) {
        value.forEach((item) => params.append(key, String(item)));
        return;
      }

      params.set(key, String(value));
    });

    const queryString = params.toString();
    return queryString ? `?${queryString}` : '';
  }
}