/**
 * API resource add DTO
 */
export interface ResourceAddDto {
  /** name */
  name: string;
  /** displayName */
  displayName: string;
  /** description */
  description?: string | null;
}
