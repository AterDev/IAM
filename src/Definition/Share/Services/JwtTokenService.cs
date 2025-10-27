using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Share.Services;

/// <summary>
/// JWT token service implementation
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IKeyManagementService _keyManagementService;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtTokenService(IConfiguration configuration, IKeyManagementService keyManagementService)
    {
        _configuration = configuration;
        _keyManagementService = keyManagementService;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string GenerateAccessToken(IEnumerable<Claim> claims, int expiresIn = 3600)
    {
        var signingCredentials = _keyManagementService.GetSigningCredentials();
        var issuer = _configuration["Jwt:Issuer"] ?? "IAM";
        var audience = _configuration["Jwt:Audience"] ?? "IAM";

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(expiresIn),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = signingCredentials,
            TokenType = "at+jwt"
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public string GenerateIdToken(IEnumerable<Claim> claims, int expiresIn = 3600)
    {
        var signingCredentials = _keyManagementService.GetSigningCredentials();
        var issuer = _configuration["Jwt:Issuer"] ?? "IAM";
        var audience = _configuration["Jwt:Audience"] ?? "IAM";

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(expiresIn),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = signingCredentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            var validationParameters = GetTokenValidationParameters();
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public IEnumerable<Claim>? GetTokenClaims(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims;
        }
        catch
        {
            return null;
        }
    }

    private TokenValidationParameters GetTokenValidationParameters()
    {
        var validationParameters = _keyManagementService.GetTokenValidationParameters();
        var issuer = _configuration["Jwt:Issuer"] ?? "IAM";
        var audience = _configuration["Jwt:Audience"] ?? "IAM";

        validationParameters.ValidIssuer = issuer;
        validationParameters.ValidAudience = audience;
        validationParameters.ValidateIssuer = true;
        validationParameters.ValidateAudience = true;
        validationParameters.ValidateLifetime = true;
        validationParameters.ValidateIssuerSigningKey = true;
        validationParameters.ClockSkew = TimeSpan.FromMinutes(5);

        return validationParameters;
    }
}
