import { PageList } from './api/models/perigon/page-list.model';

export enum PermissionType {
  Menu = 1,
  Button = 2,
  Business = 3,
}

export interface PermissionFilter {
  pageIndex?: number | null;
  pageSize?: number | null;
  orderBy?: Record<string, boolean> | null;
  clientId?: string | null;
  clientCode?: string | null;
  type?: PermissionType | null;
  parentId?: string | null;
  keyword?: string | null;
  onlyNonBusiness?: boolean | null;
}

export interface PermissionItem {
  id: string;
  code: string;
  name: string;
  displayName?: string | null;
  description?: string | null;
  type: PermissionType;
  parentId?: string | null;
  parentCode?: string | null;
  namespace?: string | null;
  resource?: string | null;
  action?: string | null;
  path?: string | null;
  icon?: string | null;
  sort: number;
  ownedClientId?: string | null;
  ownedClientCode?: string | null;
  createdTime: string;
  updatedTime: string;
}

export interface PermissionUpsertDto {
  code: string;
  name: string;
  displayName?: string | null;
  description?: string | null;
  type: PermissionType;
  parentId?: string | null;
  namespace?: string | null;
  resource?: string | null;
  action?: string | null;
  path?: string | null;
  icon?: string | null;
  sort: number;
  ownedClientId?: string | null;
}

export interface PermissionTreeNode {
  id: string;
  code: string;
  name: string;
  displayName?: string | null;
  description?: string | null;
  type: PermissionType;
  parentId?: string | null;
  namespace?: string | null;
  resource?: string | null;
  action?: string | null;
  path?: string | null;
  icon?: string | null;
  sort: number;
  ownedClientId?: string | null;
  ownedClientCode?: string | null;
  selected: boolean;
  children: PermissionTreeNode[];
}

export interface PermissionSyncNodeDto {
  code: string;
  name: string;
  displayName?: string | null;
  description?: string | null;
  type: PermissionType;
  namespace?: string | null;
  resource?: string | null;
  action?: string | null;
  path?: string | null;
  icon?: string | null;
  sort: number;
  children: PermissionSyncNodeDto[];
}

export interface ClientPermissionSyncDto {
  permissions: PermissionSyncNodeDto[];
}

export type PermissionPage = PageList<PermissionItem>;