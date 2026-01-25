/**
 * Authorization item DTO for list display
 */
export interface AuthorizationItemDto {
  /** id */
  id: string;
  /** subjectId */
  subjectId: string;
  /** clientId */
  clientId: string;
  /** status */
  status?: string | null;
  /** creationDate */
  creationDate: Date;
}
