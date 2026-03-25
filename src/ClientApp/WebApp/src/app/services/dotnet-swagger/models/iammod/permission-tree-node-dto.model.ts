import { PermissionType } from '../entity/permission-type.model';

/**
 * Permission tree node.
 */
export interface PermissionTreeNodeDto {
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
  /** path */
  path?: string | null;
  /** ownedClientId */
  ownedClientId?: string | null;
  /** ownedClientCode */
  ownedClientCode?: string | null;
  /** selected */
  selected: boolean;
  /** children */
  children: PermissionTreeNodeDto[];
}
