namespace IdentityMod;

/// <summary>
/// OAuth 2.0 and OpenID Connect constants
/// </summary>
public static class OAuthConstants
{
    /// <summary>
    /// OAuth 2.0 grant type values
    /// </summary>
    public static class GrantTypes
    {
        public const string AuthorizationCode = "authorization_code";
        public const string RefreshToken = "refresh_token";
        public const string ClientCredentials = "client_credentials";
        public const string Password = "password";
        public const string DeviceCode = "urn:ietf:params:oauth:grant-type:device_code";
    }

    /// <summary>
    /// OAuth 2.0 response type values
    /// </summary>
    public static class ResponseTypes
    {
        public const string Code = "code";
        public const string Token = "token";
        public const string IdToken = "id_token";
    }

    /// <summary>
    /// OAuth 2.0 token type values
    /// </summary>
    public static class TokenTypes
    {
        public const string AccessToken = "access_token";
        public const string RefreshToken = "refresh_token";
        public const string IdToken = "id_token";
        public const string AuthorizationCode = "authorization_code";
        public const string DeviceCode = "device_code";
        public const string UserCode = "user_code";
        public const string Bearer = "Bearer";
    }

    /// <summary>
    /// OAuth 2.0 token status values
    /// </summary>
    public static class TokenStatuses
    {
        public const string Valid = "valid";
        public const string Revoked = "revoked";
        public const string Redeemed = "redeemed";
        public const string Pending = "pending";
        public const string Denied = "denied";
    }

    /// <summary>
    /// OAuth 2.0 authorization type values
    /// </summary>
    public static class AuthorizationTypes
    {
        public const string Code = "code";
        public const string ClientCredentials = "client_credentials";
        public const string Password = "password";
        public const string DeviceCode = "device_code";
    }

    /// <summary>
    /// OAuth 2.0 authorization status values
    /// </summary>
    public static class AuthorizationStatuses
    {
        public const string Valid = "valid";
        public const string Revoked = "revoked";
        public const string Pending = "pending";
        public const string Authorized = "authorized";
        public const string Denied = "denied";
    }

    /// <summary>
    /// PKCE code challenge method values
    /// </summary>
    public static class CodeChallengeMethods
    {
        public const string Plain = "plain";
        public const string S256 = "S256";
    }

    /// <summary>
    /// OAuth 2.0 response mode values
    /// </summary>
    public static class ResponseModes
    {
        public const string Query = "query";
        public const string Fragment = "fragment";
        public const string FormPost = "form_post";
    }

    /// <summary>
    /// OAuth 2.0 error codes
    /// </summary>
    public static class ErrorCodes
    {
        public const string InvalidClient = "invalid_client";
        public const string InvalidRequest = "invalid_request";
        public const string UnsupportedResponseType = "unsupported_response_type";
        public const string InvalidScope = "invalid_scope";
        public const string InvalidGrant = "invalid_grant";
        public const string UnsupportedGrantType = "unsupported_grant_type";
        public const string ServerError = "server_error";
        public const string AuthorizationPending = "authorization_pending";
        public const string AccessDenied = "access_denied";
        public const string ExpiredToken = "expired_token";
        public const string InvalidUser = "invalid_user";
    }

    /// <summary>
    /// OpenID Connect standard scopes
    /// </summary>
    public static class Scopes
    {
        public const string OpenId = "openid";
        public const string Profile = "profile";
        public const string Email = "email";
        public const string Address = "address";
        public const string Phone = "phone";
    }

    /// <summary>
    /// Standard claim types
    /// </summary>
    public static class ClaimTypes
    {
        public const string Subject = "sub";
        public const string Name = "name";
        public const string Email = "email";
        public const string ClientId = "client_id";
        public const string Scope = "scope";
        public const string Audience = "aud";
    }
}
