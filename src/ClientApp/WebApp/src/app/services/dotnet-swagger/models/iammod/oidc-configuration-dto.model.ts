/**
 * OpenID Connect Discovery Document
 */
export interface OidcConfigurationDto {
  /** Issuer identifier for the OpenID Provider */
  issuer: string;
  /** URL of the OP's OAuth 2.0 Authorization Endpoint */
  authorization_endpoint: string;
  /** URL of the OP's OAuth 2.0 Token Endpoint */
  token_endpoint: string;
  /** URL of the OP's UserInfo Endpoint */
  userinfo_endpoint: string;
  /** URL of the OP's JSON Web Key Set document */
  jwks_uri: string;
  /** URL of the OP's OAuth 2.0 revocation endpoint */
  revocation_endpoint?: string | null;
  /** URL of the OP's OAuth 2.0 introspection endpoint */
  introspection_endpoint?: string | null;
  /** URL of the OP's OAuth 2.0 device authorization endpoint */
  device_authorization_endpoint?: string | null;
  /** URL of the OP's logout endpoint */
  end_session_endpoint?: string | null;
  /** JSON array containing a list of the OAuth 2.0 response_type values that this OP supports */
  response_types_supported: string[];
  /** JSON array containing a list of the OAuth 2.0 grant type values that this OP supports */
  grant_types_supported: string[];
  /** JSON array containing a list of the Subject Identifier types that this OP supports */
  subject_types_supported: string[];
  /** JSON array containing a list of the JWS signing algorithms (alg values) supported by the OP for the ID Token */
  id_token_signing_alg_values_supported: string[];
  /** JSON array containing a list of the OAuth 2.0 scope values that this server supports */
  scopes_supported?: string[] | null;
  /** JSON array containing a list of Client Authentication methods supported by this Token Endpoint */
  token_endpoint_auth_methods_supported?: string[] | null;
  /** JSON array containing a list of the Claim Names of the Claims that the OpenID Provider MAY be able to supply values for */
  claims_supported?: string[] | null;
  /** JSON array containing a list of Proof Key for Code Exchange (PKCE) code challenge methods supported by this authorization server */
  code_challenge_methods_supported?: string[] | null;
  /** Boolean value specifying whether the OP supports use of the request parameter */
  request_parameter_supported?: boolean | null;
  /** Boolean value specifying whether the OP supports use of the request_uri parameter */
  request_uri_parameter_supported?: boolean | null;
  /** Boolean value specifying whether the OP requires any request_uri values to be pre-registered */
  require_request_uri_registration?: boolean | null;
}
