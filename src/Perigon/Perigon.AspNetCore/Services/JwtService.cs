using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Perigon.AspNetCore.Services;

/// <summary>
/// JWT 基础服务 - 提供 token 生成、解析等通用方法
/// 注：对于 OAuth/OIDC 的完整功能，请使用 OAuthService
/// </summary>
public class JwtService
{
    private readonly IOptions<JwtOption> _options;
    private readonly ILogger<JwtService> _logger;
    public readonly int ExpiredSecond;
    public readonly int RefreshExpiredSecond;
    private readonly string _audience;
    private readonly string _issuer;
    public List<Claim>? Claims { get; set; }

    public JwtService(IOptions<JwtOption> options, ILogger<JwtService> logger)
    {
        _options = options;
        _logger = logger;
        ExpiredSecond = options.Value.ExpiredSecond;
        RefreshExpiredSecond = options.Value.RefreshExpiredSecond;
        _audience = options.Value.ValidAudiences;
        _issuer = options.Value.ValidIssuer;
    }

    /// <summary>
    /// 生成 JWT token（需外部提供签名凭证）
    /// </summary>
    /// <param name="claims">Token claims</param>
    /// <param name="signingCredentials">签名凭证（由外部提供）</param>
    /// <param name="expiresIn">过期时间（秒）</param>
    /// <returns>JWT token 字符串</returns>
    public string GetToken(
        IEnumerable<Claim> claims,
        SigningCredentials signingCredentials,
        int expiresIn = 3600
    )
    {
        try
        {
            var jwt = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(expiresIn),
                signingCredentials: signingCredentials
            );

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
            return encodedJwt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token");
            throw;
        }
    }

    /// <summary>
    /// 生成 JWT token（带角色信息，需外部提供签名凭证）
    /// </summary>
    public string GetToken(
        string id,
        string[] roles,
        SigningCredentials signingCredentials
    )
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id) };

        if (roles.Length != 0)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        if (Claims != null && Claims.Count != 0)
        {
            claims.AddRange(Claims);
        }

        return GetToken(claims, signingCredentials, ExpiredSecond);
    }

    /// <summary>
    /// 生成 JWT token（简化版，使用默认签名凭证 - 用于非 OAuth 场景）
    /// 注：这需要配置中有有效的私钥，或者应该通过 OAuthService 使用数据库中的密钥
    /// </summary>
    [Obsolete("请使用 OAuthService.GenerateTokenAsync，这个方法仅在特殊场景下使用")]
    public string GetToken(string id, string[] roles)
    {
        throw new NotImplementedException(
            "JwtService.GetToken(id, roles) 已废弃。请使用 OAuthService 进行 Token 生成。"
        );
    }

    /// <summary>
    /// 生成刷新 token
    /// </summary>
    public static string GetRefreshToken()
    {
        var guid = Guid.CreateVersion7().ToString("N");
        return guid + HashCrypto.GetRandom(32, useLow: true);
    }

    /// <summary>
    /// 解析 Token
    /// </summary>
    public ClaimsPrincipal? ParseToken(string token, TokenValidationParameters validationParameters)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }
}
