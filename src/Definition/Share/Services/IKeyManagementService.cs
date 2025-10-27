using Microsoft.IdentityModel.Tokens;

namespace Share.Services;

/// <summary>
/// Interface for key management service
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// Get signing credentials for JWT token signing
    /// </summary>
    /// <returns>Signing credentials</returns>
    SigningCredentials GetSigningCredentials();

    /// <summary>
    /// Get token validation parameters
    /// </summary>
    /// <returns>Token validation parameters</returns>
    TokenValidationParameters GetTokenValidationParameters();

    /// <summary>
    /// Rotate signing key
    /// </summary>
    /// <returns>New key ID</returns>
    Task<string> RotateKeyAsync();

    /// <summary>
    /// Get current active key ID
    /// </summary>
    /// <returns>Key ID</returns>
    string? GetCurrentKeyId();

    /// <summary>
    /// Get public key in JWK format
    /// </summary>
    /// <param name="keyId">Key ID (optional, gets current if not specified)</param>
    /// <returns>JWK JSON string</returns>
    Task<string?> GetPublicKeyJwkAsync(string? keyId = null);
}
