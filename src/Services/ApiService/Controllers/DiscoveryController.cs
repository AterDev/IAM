using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ater.Common;
using IdentityMod.Managers;
using IdentityMod.Models.OAuthDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace ApiService.Controllers;

/// <summary>
/// OpenID Connect Discovery endpoint controller
/// </summary>
/// <remarks>
/// Implements OpenID Connect Discovery 1.0 specification endpoints:
/// - Discovery document (.well-known/openid-configuration)
/// - JSON Web Key Set (JWKS) for token verification
/// - UserInfo endpoint for retrieving authenticated user claims
///
/// These endpoints enable clients to discover the OpenID Provider's capabilities
/// and obtain the public keys needed for JWT signature verification.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Produces("application/json")]
public class DiscoveryController(
    DiscoveryManager discoveryManager,
    IConfiguration configuration,
    ILogger<DiscoveryController> logger
) : ControllerBase
{
    private readonly DiscoveryManager _discoveryManager = discoveryManager;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<DiscoveryController> _logger = logger;

    /// <summary>
    /// OpenID Connect Discovery document
    /// </summary>
    /// <returns>OIDC configuration metadata</returns>
    /// <response code="200">Returns the OIDC configuration document</response>
    /// <remarks>
    /// Returns the OpenID Provider metadata as defined in OpenID Connect Discovery 1.0.
    /// This document describes the OAuth 2.0 and OpenID Connect endpoints, supported features,
    /// and capabilities of this authorization server.
    ///
    /// Clients can use this endpoint to automatically discover:
    /// - Authorization, token, and other endpoint URLs
    /// - Supported grant types and response types
    /// - Supported scopes and claims
    /// - JWKS URI for obtaining public keys
    /// - Supported algorithms and features
    ///
    /// Example:
    /// GET /.well-known/openid-configuration
    ///
    /// Response:
    /// {
    ///   "issuer": "https://auth.example.com",
    ///   "authorization_endpoint": "https://auth.example.com/connect/authorize",
    ///   "token_endpoint": "https://auth.example.com/connect/token",
    ///   "jwks_uri": "https://auth.example.com/.well-known/jwks",
    ///   ...
    /// }
    /// </remarks>
    [HttpGet("/.well-known/openid-configuration")]
    [EnableCors(AppConst.Default)]
    public ActionResult<OidcConfigurationDto> GetConfiguration()
    {
        try
        {
            // Use configured issuer URL to prevent Host header injection
            var issuer = _configuration["Authentication:Issuer"];

            // Fallback to request URL if not configured (development only)
            if (string.IsNullOrEmpty(issuer))
            {
                issuer = $"{Request.Scheme}://{Request.Host}";
                _logger.LogWarning(
                    "Issuer URL not configured, using request URL: {Issuer}. This should only happen in development.",
                    issuer
                );
            }

            var config = _discoveryManager.GetConfiguration(issuer);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate OIDC configuration");
            return Problem("Failed to generate configuration", statusCode: 500);
        }
    }

    /// <summary>
    /// JSON Web Key Set (JWKS) endpoint
    /// </summary>
    /// <returns>Public keys for JWT signature verification</returns>
    /// <response code="200">Returns the JWKS document containing public keys</response>
    /// <remarks>
    /// Returns the JSON Web Key Set (JWKS) as defined in RFC 7517.
    /// This endpoint provides the public keys that clients should use to verify
    /// the signatures of JWTs (access tokens and ID tokens) issued by this server.
    ///
    /// The JWKS contains:
    /// - Public key parameters (RSA modulus and exponent)
    /// - Key ID (kid) for matching with JWT headers
    /// - Algorithm (alg) and key type (kty) information
    /// - Key usage information (use)
    ///
    /// Clients should:
    /// 1. Fetch this document and cache the public keys
    /// 2. Match the 'kid' in JWT header with the keys in this set
    /// 3. Use the matched key to verify JWT signatures
    /// 4. Refresh periodically or when encountering unknown 'kid'
    ///
    /// Example:
    /// GET /.well-known/jwks
    ///
    /// Response:
    /// {
    ///   "keys": [
    ///     {
    ///       "kty": "RSA",
    ///       "use": "sig",
    ///       "kid": "abc123",
    ///       "alg": "RS256",
    ///       "n": "0vx7agoebGcQSuu...",
    ///       "e": "AQAB"
    ///     }
    ///   ]
    /// }
    /// </remarks>
    [HttpGet("/.well-known/jwks")]
    [EnableCors(AppConst.Default)]
    public async Task<ActionResult<JwksDto>> GetJwks()
    {
        try
        {
            var jwks = await _discoveryManager.GetJwksAsync();
            return Ok(jwks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWKS");
            return Problem("Failed to generate JWKS", statusCode: 500);
        }
    }

    /// <summary>
    /// UserInfo endpoint (OIDC)
    /// </summary>
    /// <returns>Claims about the authenticated user</returns>
    /// <response code="200">Returns user information claims</response>
    /// <response code="401">If the access token is invalid or missing</response>
    /// <response code="403">If the token does not have sufficient scope</response>
    /// <remarks>
    /// Returns claims about the authenticated End-User as defined in OpenID Connect Core 1.0.
    /// This endpoint requires a valid access token obtained through the OAuth 2.0 flow.
    ///
    /// The returned claims depend on:
    /// - The scopes granted in the access token (profile, email, phone, address)
    /// - The user's actual profile data
    /// - The client's allowed scopes
    ///
    /// Standard scopes and their claims:
    /// - profile: name, family_name, given_name, middle_name, nickname, preferred_username,
    ///   profile, picture, website, gender, birthdate, zoneinfo, locale, updated_at
    /// - email: email, email_verified
    /// - phone: phone_number, phone_number_verified
    /// - address: address (structured claim)
    ///
    /// Request must include Authorization header:
    /// Authorization: Bearer {access_token}
    ///
    /// Example:
    /// GET /connect/userinfo
    /// Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
    ///
    /// Response:
    /// {
    ///   "sub": "248289761001",
    ///   "name": "Jane Doe",
    ///   "email": "jane.doe@example.com",
    ///   "email_verified": true
    /// }
    /// </remarks>
    [HttpGet("/connect/userinfo")]
    [HttpPost("/connect/userinfo")]
    [Authorize]
    public async Task<ActionResult<UserInfoDto>> GetUserInfo()
    {
        try
        {
            // Get user ID from the token claims
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("UserInfo request with invalid or missing subject claim");
                return Unauthorized(
                    new
                    {
                        error = "invalid_token",
                        error_description = "The access token is invalid or does not contain a valid subject",
                    }
                );
            }

            // Parse scopes from token
            var scopes = ParseScopesFromToken(User);

            // Get user information
            var userInfo = await _discoveryManager.GetUserInfoAsync(userId, scopes);

            if (userInfo == null)
            {
                _logger.LogWarning("User {UserId} not found for UserInfo request", userId);
                return NotFound(
                    new
                    {
                        error = "user_not_found",
                        error_description = "The user associated with this token was not found",
                    }
                );
            }

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user info");
            return Problem("Failed to retrieve user information", statusCode: 500);
        }
    }

    /// <summary>
    /// Parse scopes from the token claims
    /// </summary>
    /// <param name="principal">The claims principal from the token</param>
    /// <returns>List of scope strings</returns>
    private static List<string> ParseScopesFromToken(ClaimsPrincipal principal)
    {
        var scopes = new List<string>();

        // Get all scope claims
        var scopeClaims = principal.FindAll("scope").Select(c => c.Value).ToList();

        if (scopeClaims.Count > 0)
        {
            scopes.AddRange(scopeClaims);
        }
        else
        {
            // Try alternative scope claim format (space-separated)
            var scopeValue = principal.FindFirst("scope")?.Value;
            if (!string.IsNullOrEmpty(scopeValue))
            {
                scopes.AddRange(scopeValue.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }
        }

        return scopes;
    }
}
