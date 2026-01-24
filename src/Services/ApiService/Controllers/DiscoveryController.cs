using IAMMod.Managers;
using IAMMod.Models.OAuthDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Perigon.AspNetCore.Constants;

namespace ApiService.Controllers;

/// <summary>
/// OpenID Connect Discovery endpoint controller
/// </summary>
/// <remarks>
/// Implements OpenID Connect Discovery 1.0 specification endpoints:
/// - Discovery document (.well-known/openid-configuration)
/// - JSON Web Key Set (JWKS) for token verification
///
/// These endpoints enable clients to discover the OpenID Provider's capabilities
/// and obtain the public keys needed for JWT signature verification.
/// </remarks>
[Route(".well-known")]
[AllowAnonymous]
[Produces("application/json")]
public class DiscoveryController(
    Share.Localizer localizer,
    DiscoveryManager discoveryManager,
    SigningKeyManager signingKeyManager,
    IConfiguration configuration,
    ILogger<DiscoveryController> logger
) : RestControllerBase(localizer)
{
    private readonly DiscoveryManager _discoveryManager = discoveryManager;
    private readonly SigningKeyManager _signingKeyManager = signingKeyManager;
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
            // 从 SigningKeyManager 获取有效公钥
            var validKeys = await _signingKeyManager.GetValidPublicKeysAsync();
            var jwks = await _discoveryManager.GetJwksAsync(validKeys);
            return Ok(jwks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWKS");
            return Problem("Failed to generate JWKS", statusCode: 500);
        }
    }
}
