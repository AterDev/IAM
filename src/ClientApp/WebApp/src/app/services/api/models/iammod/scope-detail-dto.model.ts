/**
 * Scope detail DTO
 */
export interface ScopeDetailDto {
  /** id */
  id: string;
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
  /** createdTime */
  createdTime: Date;
  /** updatedTime */
  updatedTime: Date;
}
