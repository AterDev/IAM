import { PermissionSyncNodeDto } from '../iammod/permission-sync-node-dto.model';

/**
 * Full replacement payload for client menu/button permission sync.
 */
export interface ClientPermissionSyncDto {
  /** permissions */
  permissions: PermissionSyncNodeDto[];
}
