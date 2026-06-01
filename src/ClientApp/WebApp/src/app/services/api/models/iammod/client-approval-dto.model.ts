/**
 * Review payload for approving a pending client registration.
 */
export interface ClientApprovalDto {
  /** Secret validity period in days. */
  secretExpirationDays: number;
}
