import { UserEntitlementType } from '../entity/user-entitlement-type.model';

export interface UserEntitlementDefinitionUpsertDto {
  /** displayName */
  displayName: string;
  /** description */
  description?: string | null;
  /** entitlementCode */
  entitlementCode: string;
  /** Supported user entitlement types. */
  entitlementType: UserEntitlementType;
  /** unit */
  unit: string;
}
