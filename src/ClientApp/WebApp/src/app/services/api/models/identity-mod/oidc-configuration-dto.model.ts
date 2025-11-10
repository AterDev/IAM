/**
 * OpenID Connect Discovery Document
 */
export interface OidcConfigurationDto {
  /** Issuer identifier for the OpenID Provider */
  issuer: string;
  /** URL of the OP's OAuth 2.0 Authorization Endpoint */
  authorizationEndpoint: string;
  /** URL of the OP's OAuth 2.0 Token Endpoint */
  tokenEndpoint: string;
  /** URL of the OP's UserInfo Endpoint */
  userinfoEndpoint: string;
  /** URL of the OP's JSON Web Key Set document */
  jwksUri: string;
  /** URL of the OP's OAuth 2.0 revocation endpoint */
  revocationEndpoint?: string | null;
  /** URL of the OP's OAuth 2.0 introspection endpoint */
  introspectionEndpoint?: string | null;
  /** URL of the OP's OAuth 2.0 device authorization endpoint */
  deviceAuthorizationEndpoint?: string | null;
  /** URL of the OP's logout endpoint */
  endSessionEndpoint?: string | null;
  /** JSON array containing a list of the OAuth 2.0 response_type values that this OP supports */
  responseTypesSupported: string[];
  /** JSON array containing a list of the OAuth 2.0 grant type values that this OP supports */
  grantTypesSupported: string[];
  /** JSON array containing a list of the Subject Identifier types that this OP supports */
  subjectTypesSupported: string[];
  /** JSON array containing a list of the JWS signing algorithms (alg values) supported by the OP for the ID Token */
  idTokenSigningAlgValuesSupported: string[];
  /** JSON array containing a list of the OAuth 2.0 scope values that this server supports */
  scopesSupported?: string[] | null;
  /** JSON array containing a list of Client Authentication methods supported by this Token Endpoint */
  tokenEndpointAuthMethodsSupported?: string[] | null;
  /** JSON array containing a list of the Claim Names of the Claims that the OpenID Provider MAY be able to supply values for */
  claimsSupported?: string[] | null;
  /** JSON array containing a list of Proof Key for Code Exchange (PKCE) code challenge methods supported by this authorization server */
  codeChallengeMethodsSupported?: string[] | null;
  /** Boolean value specifying whether the OP supports use of the request parameter */
  requestParameterSupported?: boolean | null;
  /** Boolean value specifying whether the OP supports use of the request_uri parameter */
  requestUriParameterSupported?: boolean | null;
  /** Boolean value specifying whether the OP requires any request_uri values to be pre-registered */
  requireRequestUriRegistration?: boolean | null;
}
