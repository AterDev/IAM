import { PermissionClaim } from '../identity-mod/permission-claim.model';

/**
 * Role grant permission DTO
 */
export interface RoleGrantPermissionDto {
  /** Permissions to grant to the role (claim type and value pairs) */
  permissions: PermissionClaim[];
}
