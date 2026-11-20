export interface UserEntitlementAddDto {
  /** entitlementDefinitionId */
  entitlementDefinitionId: string;
  /** valueLimit */
  valueLimit: any;
  /** expirationDate */
  expirationDate?: Date | null;
  /** startDate */
  startDate: Date;
}
