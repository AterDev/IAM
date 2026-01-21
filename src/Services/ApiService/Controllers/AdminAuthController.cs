using AccessMod.Managers;
using IdentityMod.Managers;
using IdentityMod.Models.AdminAuthDtos;
using Microsoft.AspNetCore.Authorization;
using Share.Services;
using System.Security.Claims;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace ApiService.Controllers;

/// <summary>
/// Admin authentication controller for management portal login
/// </summary>
[ApiController]
[Route("api/admin")]
public class AdminAuthController(
    UserManager userManager,
    RoleManager roleManager,
    OAuthService oauthService,
    SigningKeyManager signingKeyManager,
    ILogger<AdminAuthController> logger
) : ControllerBase
{
    private readonly UserManager _userManager = userManager;
    private readonly RoleManager _roleManager = roleManager;
    private readonly OAuthService _oauthService = oauthService;
    private readonly SigningKeyManager _signingKeyManager = signingKeyManager;
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
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            // Validate credentials using UserManager
            var userDetail = await _userManager.ValidateCredentialsAsync(
                loginDto.UserName,
                loginDto.Password,
                ipAddress,
                userAgent
            );

            if (userDetail == null)
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            // Get user roles
            var user = await _userManager.FindAsync(userDetail.Id);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Load user roles
            await _userManager.LoadManyAsync(user, u => u.UserRoles);

            var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
            var roles = await _roleManager.GetRoleNamesByIdsAsync(roleIds);

            var expiresIn = 3600 * 24 * 7; // 7 day

            // 获取活跃的签名密钥
            var signingKey = await _signingKeyManager.GetActiveSigningKeyAsync();
            if (signingKey == null)
            {
                return StatusCode(500, new { message = "No active signing key available" });
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var accessToken = _oauthService.GenerateToken(claims, signingKey, expiresIn);

            var response = new AdminLoginResponseDto
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = expiresIn,
                User = new AdminUserInfo
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles,
                },
            };

            _logger.LogInformation("Admin user {UserName} logged in successfully", user.UserName);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin login for user {UserName}", loginDto.UserName);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Get current admin user information
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AdminUserInfo>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindAsync(userId);
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
}
