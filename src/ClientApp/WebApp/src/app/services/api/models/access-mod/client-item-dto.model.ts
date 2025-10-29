/**
 * Client item DTO for list display
 */
export interface ClientItemDto {
  /** id */
  id: string;
  /** clientId */
  clientId: string;
  /** displayName */
  displayName: string;
  /** type */
  type?: string | null;
  /** applicationType */
  applicationType?: string | null;
  /** createdTime */
  createdTime: Date;
}
