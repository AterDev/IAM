export interface UserEntitlementAddDto {
  /** entitlementDefinitionId */
  entitlementDefinitionId: string;
  /** valueLimit */
  valueLimit: number;
  /** expirationDate */
  expirationDate?: Date | null;
  /** startDate */
  startDate: Date;
}
