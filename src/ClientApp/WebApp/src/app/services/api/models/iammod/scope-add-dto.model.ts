/**
 * Scope add DTO
 */
export interface ScopeAddDto {
  /** name */
  name: string;
  /** displayName */
  displayName: string;
  /** description */
  description?: string | null;
  /** required */
  required: boolean;
  /** emphasize */
  emphasize: boolean;
  /** claims */
  claims: string[];
}
