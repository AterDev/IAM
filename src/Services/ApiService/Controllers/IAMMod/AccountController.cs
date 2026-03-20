using IAMMod.Managers;
using IAMMod.Models.AccountDtos;
using IAMMod.Models.UserDtos;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Share.Exceptions;
using SysClaimTypes = System.Security.Claims.ClaimTypes;

namespace ApiService.Controllers.IAMMod;

/// <summary>
/// Self-service account endpoints for public authentication flows.
/// </summary>
public class AccountController(
    Localizer localizer,
    UserManager userManager,
    MfaManager mfaManager,
    IConfiguration configuration,
    ILogger<AccountController> logger)
    : RestControllerBase(localizer)
{
    private readonly UserManager _userManager = userManager;
    private readonly MfaManager _mfaManager = mfaManager;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<AccountController> _logger = logger;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDetailDto>> Register([FromBody] RegisterRequestDto dto)
    {
        try
        {
            var user = await _userManager.RegisterSelfServiceAsync(
                new UserAddDto
                {
                    UserName = dto.UserName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Password = dto.Password,
                    EmailConfirmed = false,
                    PhoneNumberConfirmed = false,
                    LockoutEnabled = true,
                },
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()
            );

            return user == null
                ? Problem(Localizer.BadRequest)
                : CreatedAtAction(nameof(Register), new { id = user.Id }, user);
        }
        catch (BusinessException ex) when (ex.StatusCodes == StatusCodes.Status400BadRequest)
        {
            return Conflict(ex.LanguageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling self-service registration");
            return Problem(Localizer.BadRequest);
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        try
        {
            await _userManager.RequestPasswordResetAsync(
                dto.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()
            );

            return Ok(new { message = "Password reset request accepted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling forgot password request");
            return Problem(Localizer.BadRequest);
        }
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        try
        {
            await _userManager.ResetPasswordAsync(
                dto.Email,
                dto.Code,
                dto.NewPassword,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()
            );

            return Ok(new { message = "Password reset completed" });
        }
        catch (BusinessException ex) when (ex.StatusCodes == StatusCodes.Status404NotFound)
        {
            return NotFound(ex.LanguageKey);
        }
        catch (BusinessException ex)
        {
            return BadRequest(ex.LanguageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return Problem(Localizer.BadRequest);
        }
    }

    [HttpGet("mfa")]
    [Authorize]
    public async Task<ActionResult<MfaStatusDto>> GetMfaStatus()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        return Ok(await _mfaManager.GetStatusAsync(userId.Value));
    }

    [HttpPost("mfa/setup")]
    [Authorize]
    public async Task<ActionResult<MfaSetupResponseDto>> BeginMfaSetup()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        return Ok(await _mfaManager.BeginSetupAsync(userId.Value, ResolveIssuer()));
    }

    [HttpPost("mfa/enable")]
    [Authorize]
    public async Task<ActionResult<MfaRecoveryCodesResponseDto>> EnableMfa([FromBody] EnableMfaRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _mfaManager.EnableAsync(userId.Value, dto.Code));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ex.LanguageKey);
        }
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    public async Task<ActionResult> DisableMfa([FromBody] DisableMfaRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            await _mfaManager.DisableAsync(userId.Value, dto.Code);
            return Ok(new { message = "MFA disabled" });
        }
        catch (BusinessException ex)
        {
            return BadRequest(ex.LanguageKey);
        }
    }

    [HttpPost("mfa/recovery-codes/regenerate")]
    [Authorize]
    public async Task<ActionResult<MfaRecoveryCodesResponseDto>> RegenerateRecoveryCodes([FromBody] RegenerateRecoveryCodesRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _mfaManager.RegenerateRecoveryCodesAsync(userId.Value, dto.Code));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ex.LanguageKey);
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(SysClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(OAuthConst.JwtClaimNames.Subject);

        return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }

    private string ResolveIssuer()
    {
        var configuredIssuer = _configuration["Authentication:Issuer"]
            ?? _configuration["Authentication:Jwt:ValidIssuer"];

        if (Uri.TryCreate(configuredIssuer, UriKind.Absolute, out var issuerUri))
        {
            return issuerUri.ToString().TrimEnd('/');
        }

        return $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
    }
}