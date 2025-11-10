import { AddressClaimDto } from '../identity-mod/address-claim-dto.model';

/**
 * UserInfo endpoint response
 */
export interface UserInfoDto {
  /** Subject - Identifier for the End-User at the Issuer */
  sub: string;
  /** End-User's full name in displayable form */
  name?: string | null;
  /** Given name(s) or first name(s) of the End-User */
  givenName?: string | null;
  /** Surname(s) or last name(s) of the End-User */
  familyName?: string | null;
  /** Middle name(s) of the End-User */
  middleName?: string | null;
  /** Casual name of the End-User */
  nickname?: string | null;
  /** Shorthand name by which the End-User wishes to be referred to */
  preferredUsername?: string | null;
  /** URL of the End-User's profile page */
  profile?: string | null;
  /** URL of the End-User's profile picture */
  picture?: string | null;
  /** URL of the End-User's Web page or blog */
  website?: string | null;
  /** End-User's preferred e-mail address */
  email?: string | null;
  /** True if the End-User's e-mail address has been verified */
  emailVerified?: boolean | null;
  /** End-User's gender */
  gender?: string | null;
  /** End-User's birthday, represented as YYYY-MM-DD */
  birthdate?: string | null;
  /** String from zoneinfo time zone database representing the End-User's time zone */
  zoneinfo?: string | null;
  /** End-User's locale, represented as a BCP47 language tag */
  locale?: string | null;
  /** End-User's preferred telephone number */
  phoneNumber?: string | null;
  /** True if the End-User's phone number has been verified */
  phoneNumberVerified?: boolean | null;
  /** Address claim */
  address: AddressClaimDto;
  /** Time the End-User's information was last updated (Unix timestamp) */
  updatedAt?: number | null;
}
