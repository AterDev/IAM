namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// UserInfo endpoint response
/// </summary>
/// <remarks>
/// Represents the claims about the authenticated End-User as defined in
/// OpenID Connect Core 1.0 specification, Section 5.3.2.
/// The contents depend on the requested scopes and the user's profile.
/// </remarks>
public class UserInfoDto
{
    /// <summary>
    /// Subject - Identifier for the End-User at the Issuer
    /// </summary>
    public required string Sub { get; set; }

    /// <summary>
    /// End-User's full name in displayable form
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Given name(s) or first name(s) of the End-User
    /// </summary>
    public string? GivenName { get; set; }

    /// <summary>
    /// Surname(s) or last name(s) of the End-User
    /// </summary>
    public string? FamilyName { get; set; }

    /// <summary>
    /// Middle name(s) of the End-User
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Casual name of the End-User
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// Shorthand name by which the End-User wishes to be referred to
    /// </summary>
    public string? PreferredUsername { get; set; }

    /// <summary>
    /// URL of the End-User's profile page
    /// </summary>
    public string? Profile { get; set; }

    /// <summary>
    /// URL of the End-User's profile picture
    /// </summary>
    public string? Picture { get; set; }

    /// <summary>
    /// URL of the End-User's Web page or blog
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// End-User's preferred e-mail address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// True if the End-User's e-mail address has been verified
    /// </summary>
    public bool? EmailVerified { get; set; }

    /// <summary>
    /// End-User's gender
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// End-User's birthday, represented as YYYY-MM-DD
    /// </summary>
    public string? Birthdate { get; set; }

    /// <summary>
    /// String from zoneinfo time zone database representing the End-User's time zone
    /// </summary>
    public string? Zoneinfo { get; set; }

    /// <summary>
    /// End-User's locale, represented as a BCP47 language tag
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// End-User's preferred telephone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// True if the End-User's phone number has been verified
    /// </summary>
    public bool? PhoneNumberVerified { get; set; }

    /// <summary>
    /// End-User's preferred postal address
    /// </summary>
    public AddressClaimDto? Address { get; set; }

    /// <summary>
    /// Time the End-User's information was last updated (Unix timestamp)
    /// </summary>
    public long? UpdatedAt { get; set; }
}

/// <summary>
/// Address claim
/// </summary>
public class AddressClaimDto
{
    /// <summary>
    /// Full mailing address, formatted for display
    /// </summary>
    public string? Formatted { get; set; }

    /// <summary>
    /// Full street address component
    /// </summary>
    public string? StreetAddress { get; set; }

    /// <summary>
    /// City or locality component
    /// </summary>
    public string? Locality { get; set; }

    /// <summary>
    /// State, province, prefecture, or region component
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Zip code or postal code component
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country name component
    /// </summary>
    public string? Country { get; set; }
}
