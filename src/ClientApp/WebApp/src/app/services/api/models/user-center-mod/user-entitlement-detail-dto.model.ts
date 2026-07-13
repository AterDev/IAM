import { UserEntitlementType } from '../entity/user-entitlement-type.model';

export interface UserEntitlementDetailDto {
  /** id */
  id: string;
  /** userId */
  userId: string;
  /** entitlementDefinitionId */
  entitlementDefinitionId: string;
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
  /** valueLimit */
  valueLimit: number;
  /** currentValue */
  currentValue: number;
  /** expirationDate */
  expirationDate?: Date | null;
  /** startDate */
  startDate: Date;
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
