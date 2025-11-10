/**
 * Address claim
 */
export interface AddressClaimDto {
  /** Full mailing address, formatted for display */
  formatted?: string | null;
  /** Full street address component */
  streetAddress?: string | null;
  /** City or locality component */
  locality?: string | null;
  /** State, province, prefecture, or region component */
  region?: string | null;
  /** Zip code or postal code component */
  postalCode?: string | null;
  /** Country name component */
  country?: string | null;
}
