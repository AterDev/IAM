import { PermissionType } from '../entity/permission-type.model';

/**
 * Current user's effective permission item.
 */
export interface UserPermissionDto {
  /** code */
  code: string;
  /** Permission type. */
  type: PermissionType;
  /** ownedClientCode */
  ownedClientCode?: string | null;
}
