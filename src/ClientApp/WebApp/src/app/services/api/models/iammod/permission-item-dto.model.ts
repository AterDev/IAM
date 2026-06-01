import { PermissionType } from '../entity/permission-type.model';

/**
 * Permission list item.
 */
export interface PermissionItemDto {
  /** id */
  id: string;
  /** code */
  code: string;
  /** name */
  name: string;
  /** description */
  description?: string | null;
  /** Permission type. */
  type: PermissionType;
  /** parentId */
  parentId?: string | null;
  /** parentCode */
  parentCode?: string | null;
  /** path */
  path?: string | null;
  /** ownedClientId */
  ownedClientId?: string | null;
  /** ownedClientCode */
  ownedClientCode?: string | null;
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
