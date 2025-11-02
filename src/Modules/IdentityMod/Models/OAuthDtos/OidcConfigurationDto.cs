namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// OpenID Connect Discovery Document
/// </summary>
/// <remarks>
/// Represents the metadata about the OpenID Provider as defined in
/// OpenID Connect Discovery 1.0 specification.
/// This document is typically served at /.well-known/openid-configuration
/// </remarks>
public class OidcConfigurationDto
{
    /// <summary>
    /// Issuer identifier for the OpenID Provider
    /// </summary>
    public required string Issuer { get; set; }

    /// <summary>
    /// URL of the OP's OAuth 2.0 Authorization Endpoint
    /// </summary>
    public required string AuthorizationEndpoint { get; set; }

    /// <summary>
    /// URL of the OP's OAuth 2.0 Token Endpoint
    /// </summary>
    public required string TokenEndpoint { get; set; }

    /// <summary>
    /// URL of the OP's UserInfo Endpoint
    /// </summary>
    public required string UserinfoEndpoint { get; set; }

    /// <summary>
    /// URL of the OP's JSON Web Key Set document
    /// </summary>
    public required string JwksUri { get; set; }

    /// <summary>
    /// URL of the OP's OAuth 2.0 revocation endpoint
    /// </summary>
    public string? RevocationEndpoint { get; set; }

    /// <summary>
    /// URL of the OP's OAuth 2.0 introspection endpoint
    /// </summary>
    public string? IntrospectionEndpoint { get; set; }

    /// <summary>
    /// URL of the OP's OAuth 2.0 device authorization endpoint
    /// </summary>
    public string? DeviceAuthorizationEndpoint { get; set; }

    /// <summary>
    /// URL of the OP's logout endpoint
    /// </summary>
    public string? EndSessionEndpoint { get; set; }

    /// <summary>
    /// JSON array containing a list of the OAuth 2.0 response_type values that this OP supports
    /// </summary>
    public required List<string> ResponseTypesSupported { get; set; }

    /// <summary>
    /// JSON array containing a list of the OAuth 2.0 grant type values that this OP supports
    /// </summary>
    public required List<string> GrantTypesSupported { get; set; }

    /// <summary>
    /// JSON array containing a list of the Subject Identifier types that this OP supports
    /// </summary>
    public required List<string> SubjectTypesSupported { get; set; }

    /// <summary>
    /// JSON array containing a list of the JWS signing algorithms (alg values) supported by the OP for the ID Token
    /// </summary>
    public required List<string> IdTokenSigningAlgValuesSupported { get; set; }

    /// <summary>
    /// JSON array containing a list of the OAuth 2.0 scope values that this server supports
    /// </summary>
    public List<string>? ScopesSupported { get; set; }

    /// <summary>
    /// JSON array containing a list of Client Authentication methods supported by this Token Endpoint
    /// </summary>
    public List<string>? TokenEndpointAuthMethodsSupported { get; set; }

    /// <summary>
    /// JSON array containing a list of the Claim Names of the Claims that the OpenID Provider MAY be able to supply values for
    /// </summary>
    public List<string>? ClaimsSupported { get; set; }

    /// <summary>
    /// JSON array containing a list of Proof Key for Code Exchange (PKCE) code challenge methods supported by this authorization server
    /// </summary>
    public List<string>? CodeChallengeMethodsSupported { get; set; }

    /// <summary>
    /// Boolean value specifying whether the OP supports use of the request parameter
    /// </summary>
    public bool? RequestParameterSupported { get; set; }

    /// <summary>
    /// Boolean value specifying whether the OP supports use of the request_uri parameter
    /// </summary>
    public bool? RequestUriParameterSupported { get; set; }

    /// <summary>
    /// Boolean value specifying whether the OP requires any request_uri values to be pre-registered
    /// </summary>
    public bool? RequireRequestUriRegistration { get; set; }
}
