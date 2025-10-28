/**
 * Client secret rotation response DTO
 */
export interface ClientSecretDto {
  /** New client secret (only returned once) */
  secret: string;
}
