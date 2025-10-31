using System.Security.Cryptography;
using System.Text;

namespace IdentityMod.Services;

/// <summary>
/// Service for OAuth/OIDC operations
/// </summary>
public class OAuthService(ILogger<OAuthService> logger)
{
    /// <summary>
    /// Validate PKCE challenge
    /// </summary>
    public bool ValidatePkce(string verifier, string challenge, string method)
    {
        if (method == CodeChallengeMethods.Plain)
        {
            return verifier == challenge;
        }
        else if (method == CodeChallengeMethods.S256)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
            var computed = Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            return computed == challenge;
        }

        return false;
    }

    /// <summary>
    /// Generate authorization code
    /// </summary>
    public string GenerateAuthorizationCode()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Generate token reference
    /// </summary>
    public string GenerateTokenReference()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}