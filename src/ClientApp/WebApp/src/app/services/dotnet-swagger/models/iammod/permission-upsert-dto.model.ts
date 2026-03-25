import { PermissionType } from '../entity/permission-type.model';

/**
 * Permission create/update payload.
 */
export interface PermissionUpsertDto {
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
  /** path */
  path?: string | null;
  /** ownedClientId */
  ownedClientId?: string | null;
}
