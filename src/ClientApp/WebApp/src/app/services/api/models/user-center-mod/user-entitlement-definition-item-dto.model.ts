import { UserEntitlementType } from '../entity/user-entitlement-type.model';

export interface UserEntitlementDefinitionItemDto {
  /** id */
  id: string;
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
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
