using System.Security.Claims;

namespace Share.Services;

/// <summary>
/// Interface for JWT token service
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate an access token
    /// </summary>
    /// <param name="claims">Token claims</param>
    /// <param name="expiresIn">Expiration time in seconds (default: 3600)</param>
    /// <returns>JWT token string</returns>
    string GenerateAccessToken(IEnumerable<Claim> claims, int expiresIn = 3600);

    /// <summary>
    /// Generate an ID token (OIDC)
    /// </summary>
    /// <param name="claims">Token claims</param>
    /// <param name="expiresIn">Expiration time in seconds (default: 3600)</param>
    /// <returns>JWT token string</returns>
    string GenerateIdToken(IEnumerable<Claim> claims, int expiresIn = 3600);

    /// <summary>
    /// Validate a JWT token
    /// </summary>
    /// <param name="token">JWT token string</param>
    /// <returns>ClaimsPrincipal if valid, null otherwise</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Get claims from a JWT token without validation
    /// </summary>
    /// <param name="token">JWT token string</param>
    /// <returns>Collection of claims</returns>
    IEnumerable<Claim>? GetTokenClaims(string token);
}
