import { PermissionType } from '../entity/permission-type.model';

/**
 * Client menu/button permission sync node.
 */
export interface PermissionSyncNodeDto {
  /** code */
  code: string;
  /** name */
  name: string;
  /** description */
  description?: string | null;
  /** Permission type. */
  type: PermissionType;
  /** path */
  path?: string | null;
  /** children */
  children: PermissionSyncNodeDto[];
}
