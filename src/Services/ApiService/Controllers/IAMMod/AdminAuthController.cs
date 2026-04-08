using IAMMod.Managers;
using IAMMod.Models.AdminAuthDtos;
using IAMMod.Models.LoginSessionDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Perigon.AspNetCore.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace ApiService.Controllers.IAMMod;

/// <summary>
/// Admin authentication controller for management portal login
/// </summary>
[Route("api/admin")]
public class AdminAuthController(
    Localizer localizer,
    UserManager userManager,
    RoleManager roleManager,
    SigningKeyManager signingKeyManager,
    JwtService jwtService,
    SessionManager sessionManager,
    TokenManager tokenManager,
    IUserContext user,
    ILogger<AdminAuthController> logger
) : RestControllerBase(localizer)
{
    private readonly UserManager _userManager = userManager;
    private readonly RoleManager _roleManager = roleManager;
    private readonly JwtService _jwtService = jwtService;
    private readonly SessionManager _sessionManager = sessionManager;
    private readonly TokenManager _tokenManager = tokenManager;
    private readonly IUserContext _user = user;
    private readonly ILogger<AdminAuthController> _logger = logger;

    /// <summary>
    /// Admin login endpoint
    /// </summary>
    /// <param name="loginDto">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AdminLoginResponseDto>> Login([FromBody] AdminLoginDto loginDto)
    {

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        // Validate credentials using UserManager
        var userDetail = await _userManager.ValidateCredentialsAsync(
            loginDto.Email,
            loginDto.Password,
            ipAddress,
            userAgent
        );

        if (userDetail == null)
        {
            return Problem(Localizer.InvalidUserOrPassword);
        }

        // Get user roles
        var user = await _userManager.FindAsync(userDetail.Id);
        if (user == null)
        {
            return Problem(Localizer.NotFoundUser);
        }

        // Load user roles
        await _userManager.LoadManyAsync(user, u => u.UserRoles);

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleManager.GetRoleNamesByIdsAsync(roleIds);

        var expiresIn = _jwtService.ExpiredSecond;
        var sessionId = Guid.CreateVersion7().ToString();
        var sessionExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

        await _sessionManager.AddAsync(
            new LoginSessionAddDto
            {
                UserId = user.Id,
                SessionId = sessionId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceInfo = userAgent,
                ExpirationTime = sessionExpiresAt,
            },
            ipAddress,
            userAgent
        );

        _jwtService.Claims =
        [
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Sid, sessionId),
        ];

        var signingKey = await signingKeyManager.GetActiveSigningKeyAsync();
        if (signingKey == null)
        {
            _logger.LogError("No active signing key found for JWT generation.");
            return Problem("no active signing key found for JWT generation.");
        }
        var rsa = HashCrypto.ImportRsaPrivateKey(signingKey.PrivateKey);
        var rsaKey = new RsaSecurityKey(rsa) { KeyId = signingKey.KeyId };

        var signingCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory
            {
                CacheSignatureProviders = false
            }
        };

        var accessToken = _jwtService.GetToken(signingCredentials, user.Id.ToString(), [.. roles]);
        return new AdminLoginResponseDto
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            SessionId = sessionId,
            User = new AdminUserInfo
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles,
            },
        };
    }

    /// <summary>
    /// Get current admin user information
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AdminUserInfo>> GetCurrentUser()
    {
        if (_user.UserId == Guid.Empty)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindAsync(_user.UserId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Load user roles
        await _userManager.LoadManyAsync(user, u => u.UserRoles);

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleManager.GetRoleNamesByIdsAsync(roleIds);

        var userInfo = new AdminUserInfo
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = roles,
        };

        return Ok(userInfo);
    }

    /// <summary>
    /// Logout current admin user and revoke server-side session artifacts.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        if (_user.UserId == Guid.Empty)
        {
            return Unauthorized();
        }

        var sid = User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        if (!string.IsNullOrWhiteSpace(sid))
        {
            var session = await _sessionManager.GetBySessionIdAsync(sid);
            if (session != null)
            {
                await _sessionManager.RevokeSessionAsync(
                    session.Id,
                    _user.UserId.ToString(),
                    ipAddress,
                    userAgent
                );
            }
        }
        else
        {
            await _sessionManager.RevokeAllUserSessionsAsync(
                _user.UserId,
                null,
                _user.UserId.ToString(),
                ipAddress,
                userAgent
            );
        }

        await HttpContext.SignOutAsync();

        return Ok(new { message = "Logged out successfully" });
    }
}
