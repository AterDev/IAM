using System.Security.Cryptography;
using Entity.IAMMod;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IAMMod.Managers;

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
    public async Task<OidcConfigurationDto> GetConfigurationAsync(string issuer)
    {
        var baseUrl = issuer.TrimEnd('/');
        var scopes = await _dbContext.ApiScopes
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Name)
            .Select(s => s.Name)
            .ToListAsync();

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
            ResponseTypesSupported = ["code"],
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
            ScopesSupported = scopes,
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
    /// 注意：密钥由调用者提供，符合 Controller 协调 Manager 的规则。
    /// </summary>
    /// <returns>JWKS document with public keys</returns>
    public Task<JwksDto> GetJwksAsync(IEnumerable<SigningKey> signingKeys)
    {
        var keys = new List<JsonWebKeyDto>();

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
                _logger.LogError(ex, "Failed to convert signing key {KeyId} to JWK", key.KeyId);
            }
        }

        return Task.FromResult(new JwksDto { Keys = keys });
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

            rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
            var parameters = rsa.ExportParameters(false);

            // Validate that required parameters are present
            return parameters.Modulus == null || parameters.Exponent == null
                ? null
                : new JsonWebKeyDto
                {
                    Kty = "RSA",
                    Use = "sig",
                    Kid = key.KeyId,
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
