/**
 * Metadata for a previously issued client secret.
 */
export interface ClientSecretHistoryDto {
  /** id */
  id: string;
  /** lastFour */
  lastFour: string;
  /** issuedTime */
  issuedTime: Date;
  /** expiresAt */
  expiresAt?: Date | null;
  /** revokedAt */
  revokedAt?: Date | null;
  /** isActive */
  isActive: boolean;
}
