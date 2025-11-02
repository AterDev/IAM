using System.Security.Cryptography;
using Entity.CommonMod;
using IdentityMod.Models.OAuthDtos;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IdentityMod.Managers;

/// <summary>
/// Manager for OIDC Discovery and JWKS endpoints
/// </summary>
public class DiscoveryManager(
    DefaultDbContext context,
    ILogger<DiscoveryManager> logger,
    IConfiguration configuration
) : ManagerBase<DefaultDbContext>(context, logger)
{
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Get OpenID Connect configuration document
    /// </summary>
    /// <param name="issuer">The issuer URL (must be validated by caller)</param>
    /// <returns>OIDC configuration document</returns>
    public OidcConfigurationDto GetConfiguration(string issuer)
    {
        var baseUrl = issuer.TrimEnd('/');

        return new OidcConfigurationDto
        {
            Issuer = baseUrl,
            AuthorizationEndpoint = $"{baseUrl}/connect/authorize",
            TokenEndpoint = $"{baseUrl}/connect/token",
            UserinfoEndpoint = $"{baseUrl}/connect/userinfo",
            JwksUri = $"{baseUrl}/.well-known/jwks",
            RevocationEndpoint = $"{baseUrl}/connect/revoke",
            IntrospectionEndpoint = $"{baseUrl}/connect/introspect",
            DeviceAuthorizationEndpoint = $"{baseUrl}/connect/device",
            EndSessionEndpoint = $"{baseUrl}/connect/logout",
            ResponseTypesSupported =
            [
                "code",
                "token",
                "id_token",
                "code id_token",
                "code token",
                "id_token token",
                "code id_token token",
            ],
            GrantTypesSupported =
            [
                "authorization_code",
                "client_credentials",
                "refresh_token",
                "password",
                "urn:ietf:params:oauth:grant-type:device_code",
            ],
            SubjectTypesSupported = ["public"],
            IdTokenSigningAlgValuesSupported = ["RS256"],
            ScopesSupported = ["openid", "profile", "email", "phone", "address", "offline_access"],
            TokenEndpointAuthMethodsSupported = ["client_secret_basic", "client_secret_post"],
            ClaimsSupported =
            [
                "sub",
                "name",
                "given_name",
                "family_name",
                "middle_name",
                "nickname",
                "preferred_username",
                "profile",
                "picture",
                "website",
                "email",
                "email_verified",
                "gender",
                "birthdate",
                "zoneinfo",
                "locale",
                "phone_number",
                "phone_number_verified",
                "address",
                "updated_at",
            ],
            CodeChallengeMethodsSupported = ["plain", "S256"],
            RequestParameterSupported = false,
            RequestUriParameterSupported = false,
            RequireRequestUriRegistration = false,
        };
    }

    /// <summary>
    /// Get JSON Web Key Set (JWKS) containing public keys for token verification
    /// </summary>
    /// <returns>JWKS document with public keys</returns>
    public async Task<JwksDto> GetJwksAsync()
    {
        var keys = new List<JsonWebKeyDto>();

        // Get current signing keys from database
        var signingKeys = await _dbContext
            .SigningKeys.Where(k => !k.IsDeleted && k.ExpirationDate > DateTime.UtcNow)
            .OrderByDescending(k => k.CreatedTime)
            .Take(2) // Include current and previous key for rotation period
            .ToListAsync();

        foreach (var key in signingKeys)
        {
            try
            {
                var jwk = ConvertToJsonWebKey(key);
                if (jwk != null)
                {
                    keys.Add(jwk);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert signing key {KeyId} to JWK", key.Id);
            }
        }

        return new JwksDto { Keys = keys };
    }

    /// <summary>
    /// Convert SigningKey entity to JsonWebKeyDto
    /// </summary>
    private static JsonWebKeyDto? ConvertToJsonWebKey(SigningKey key)
    {
        if (string.IsNullOrEmpty(key.PublicKey))
        {
            return null;
        }

        try
        {
            // Import RSA public key
            using var rsa = RSA.Create();
            var publicKeyBytes = Convert.FromBase64String(key.PublicKey);

            // Validate key size (minimum 2048 bits for RSA)
            if (publicKeyBytes.Length < 256) // 2048 bits = 256 bytes minimum
            {
                return null;
            }

            rsa.ImportRSAPublicKey(publicKeyBytes, out _);
            var parameters = rsa.ExportParameters(false);

            // Validate that required parameters are present
            return parameters.Modulus == null || parameters.Exponent == null
                ? null
                : new JsonWebKeyDto
                {
                    Kty = "RSA",
                    Use = "sig",
                    Kid = key.Id.ToString(),
                    Alg = key.Algorithm ?? "RS256",
                    N = Base64UrlEncoder.Encode(parameters.Modulus),
                    E = Base64UrlEncoder.Encode(parameters.Exponent),
                };
        }
        catch (CryptographicException)
        {
            // Invalid key format
            return null;
        }
        catch (FormatException)
        {
            // Invalid base64 string
            return null;
        }
    }

    /// <summary>
    /// Get user information based on access token claims
    /// </summary>
    /// <param name="userId">User ID from token subject claim</param>
    /// <param name="scopes">Requested scopes</param>
    /// <returns>User information DTO</returns>
    public async Task<UserInfoDto?> GetUserInfoAsync(Guid userId, List<string> scopes)
    {
        var user = await _dbContext
            .Users.Where(u => u.Id == userId && !u.IsDeleted)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return null;
        }

        var userInfo = new UserInfoDto { Sub = user.Id.ToString() };

        // Add profile claims if 'profile' scope is included
        if (scopes.Contains("profile"))
        {
            userInfo.Name = user.UserName;
            userInfo.PreferredUsername = user.UserName;
            // Additional profile fields can be added based on user properties
        }

        // Add email claims if 'email' scope is included
        if (scopes.Contains("email"))
        {
            userInfo.Email = user.Email;
            userInfo.EmailVerified = user.EmailConfirmed;
        }

        // Add phone claims if 'phone' scope is included
        if (scopes.Contains("phone"))
        {
            userInfo.PhoneNumber = user.PhoneNumber;
            userInfo.PhoneNumberVerified = user.PhoneNumberConfirmed;
        }

        return userInfo;
    }
}
