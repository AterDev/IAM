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
  type?: number | null;
  /** applicationType */
  applicationType?: number | null;
  /** createdTime */
  createdTime: Date;
}
