using System.Security.Claims;
using IdentityMod.Managers;
using IdentityMod.Models.AdminAuthDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Share.Services;

namespace ApiService.Controllers;

/// <summary>
/// Admin authentication controller for management portal login
/// </summary>
[ApiController]
[Route("api/admin")]
public class AdminAuthController(
    UserManager userManager,
    RoleManager roleManager,
    IJwtTokenService jwtTokenService,
    ILogger<AdminAuthController> logger
) : ControllerBase
{
    private readonly UserManager _userManager = userManager;
    private readonly RoleManager _roleManager = roleManager;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
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
                return Unauthorized(new { message = _userManager.ErrorMsg ?? "Invalid username or password" });
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

            // Generate JWT claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new("sub", user.Id.ToString()),
                new("preferred_username", user.UserName)
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
                claims.Add(new Claim("email", user.Email));
            }

            // Add roles to claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Generate JWT token with 2 hours expiration
            var expiresIn = 7200; // 2 hours
            var accessToken = _jwtTokenService.GenerateAccessToken(claims, expiresIn);

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
                    Roles = roles
                }
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
            Roles = roles
        };

        return Ok(userInfo);
    }
}
