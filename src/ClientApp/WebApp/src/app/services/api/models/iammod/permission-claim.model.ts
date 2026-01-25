/**
 * Permission claim
 */
export interface PermissionClaim {
  /** Claim type (e.g., "permissions", "resource", etc.) */
  claimType: string;
  /** Claim value (e.g., "users.read", "users.write", etc.) */
  claimValue: string;
}
