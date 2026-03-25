/**
 * Scope update DTO
 */
export interface ScopeUpdateDto {
  /** displayName */
  displayName?: string | null;
  /** description */
  description?: string | null;
  /** required */
  required?: boolean | null;
  /** emphasize */
  emphasize?: boolean | null;
  /** claims */
  claims?: string[] | null;
}
