using IAMMod.Managers;
using IAMMod.Models.AccountDtos;
using IAMMod.Models.UserDtos;
using Microsoft.AspNetCore.Authorization;
using Share.Exceptions;

namespace ApiService.Controllers.IAMMod;

/// <summary>
/// Self-service account endpoints for public authentication flows.
/// </summary>
[AllowAnonymous]
public class AccountController(Localizer localizer, UserManager userManager, ILogger<AccountController> logger)
    : RestControllerBase(localizer)
{
    private readonly UserManager _userManager = userManager;
    private readonly ILogger<AccountController> _logger = logger;

    [HttpPost("register")]
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
}