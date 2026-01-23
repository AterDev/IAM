using Entity.CommonMod;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Perigon.AspNetCore.Options;
using Share.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Share.Services;

/// <summary>
/// OAuth/OIDC 核心业务逻辑服务
/// </summary>
public class OAuthService(ILogger<OAuthService> logger, IOptions<JwtOption> options)
{
    private readonly ILogger<OAuthService> _logger = logger;
    private readonly string Audience = options.Value.ValidAudiences;
    private readonly string Issuer = options.Value.ValidIssuer;

    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    public string GenerateToken(
        IEnumerable<Claim> claims,
        SigningKey signingKey,
        int expiresInSeconds = 3600,
        string? issuer = null
    )
    {
        issuer ??= Issuer;
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentNullException.ThrowIfNull(signingKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expiresInSeconds);

        RSA rsa = HashCrypto.ImportRsaPrivateKey(signingKey.PrivateKey);
        try
        {
            var rsaKey = new RsaSecurityKey(rsa) { KeyId = signingKey.KeyId };
            var signingCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory
                {
                    CacheSignatureProviders = false
                }
            };

            claims = claims.Append(new Claim(OAuthConst.ClaimTypes.Audience, Audience));
            var jwt = new JwtSecurityToken(
                issuer: issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(expiresInSeconds),
                signingCredentials: signingCredentials
            );

            var handler = new JwtSecurityTokenHandler();
            var token = handler.WriteToken(jwt);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token");
            throw;
        }
        finally
        {
            rsa.Dispose();
        }
    }

    /// <summary>
    /// 验证 PKCE
    /// </summary>
    public static bool ValidatePkce(string verifier, string challenge, string method)
    {
        if (method.Equals("plain", StringComparison.OrdinalIgnoreCase))
        {
            return verifier == challenge;
        }

        if (method.Equals("S256", StringComparison.OrdinalIgnoreCase))
        {
            var sha256 = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
            var computed = Base64UrlTextEncoder.Encode(sha256);
            return computed == challenge;
        }

        return false;
    }

    /// <summary>
    /// 生成授权码
    /// </summary>
    public static string GenerateAuthorizationCode()
    {
        return GenerateRandomString(32);
    }

    /// <summary>
    /// 生成 Token 引用/随机标识符
    /// </summary>
    public static string GenerateTokenReference()
    {
        return GenerateRandomString(32);
    }

    private static string GenerateRandomString(int bytes)
    {
        var buffer = new byte[bytes];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(buffer);
        return Base64UrlTextEncoder.Encode(buffer);
    }
}

/// <summary>
/// 简单的 Base64Url 编码器，避免依赖特定的库
/// </summary>
internal static class Base64UrlTextEncoder
{
    public static string Encode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
