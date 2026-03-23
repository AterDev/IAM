using IAMMod.Managers;
using IAMMod.Models.LoginSessionDtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Share.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using SysClaimTypes = System.Security.Claims.ClaimTypes;

namespace ApiService.Pages.Account;

public class MfaModel(
    UserManager userManager,
    SessionManager sessionManager,
    MfaManager mfaManager,
    ILogger<MfaModel> logger) : PageModel
{
    private readonly UserManager _userManager = userManager;
    private readonly SessionManager _sessionManager = sessionManager;
    private readonly MfaManager _mfaManager = mfaManager;
    private readonly ILogger<MfaModel> _logger = logger;

    private const string PendingMfaUserIdKey = "PendingMfaUserId";
    private const string PendingMfaUserNameKey = "PendingMfaUserName";
    private const string PendingMfaRememberMeKey = "PendingMfaRememberMe";
    private const string PendingMfaReturnUrlKey = "PendingMfaReturnUrl";
    private const string PendingMfaExpiresAtKey = "PendingMfaExpiresAt";

    [BindProperty]
    [Required(ErrorMessage = "请输入验证码或恢复码")]
    [Display(Name = "验证码或恢复码")]
    public string Code { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true, Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true, Name = "rememberMe")]
    public bool RememberMe { get; set; }

    public string UserName { get; private set; } = string.Empty;

    public IActionResult OnGet()
    {
        if (!TryLoadPendingState(out _, out var userName, out _, out var returnUrl))
        {
            return RedirectToPage("/Account/Login", new { returnUrl = ReturnUrl });
        }

        UserName = userName;
        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            TryLoadPendingState(out _, out var currentUserName, out _, out _);
            UserName = currentUserName;
            return Page();
        }

        if (!TryLoadPendingState(out var userId, out var userName, out var rememberMe, out var returnUrl))
        {
            return RedirectToPage("/Account/Login", new { returnUrl = ReturnUrl });
        }

        UserName = userName;
        ReturnUrl = returnUrl;
        RememberMe = rememberMe;

        try
        {
            var verified = await _mfaManager.VerifyLoginChallengeAsync(userId, Code);
            if (!verified)
            {
                ModelState.AddModelError(string.Empty, "验证码或恢复码无效");
                return Page();
            }

            var user = await _userManager.GetDetailAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "用户不存在");
                return Page();
            }

            var sessionId = Guid.CreateVersion7().ToString();
            var sessionExpiresAt = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : DateTimeOffset.UtcNow.AddHours(2);

            await _sessionManager.AddAsync(
                new LoginSessionAddDto
                {
                    UserId = user.Id,
                    SessionId = sessionId,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                    DeviceInfo = HttpContext.Request.Headers.UserAgent.ToString(),
                    ExpirationTime = sessionExpiresAt,
                },
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString());

            var claims = new List<Claim>
            {
                new(SysClaimTypes.NameIdentifier, user.Id.ToString()),
                new(SysClaimTypes.Name, user.UserName),
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Sid, sessionId),
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(SysClaimTypes.Email, user.Email));
            }

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = sessionExpiresAt,
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                authProperties);

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("SessionId", sessionId);

            ClearPendingState();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("~/");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MFA verification failed for user {UserId}", userId);
            ModelState.AddModelError(string.Empty, "验证过程中发生错误，请稍后重试");
            return Page();
        }
    }

    private bool TryLoadPendingState(out Guid userId, out string userName, out bool rememberMe, out string? returnUrl)
    {
        userId = Guid.Empty;
        userName = string.Empty;
        rememberMe = false;
        returnUrl = null;

        var userIdValue = HttpContext.Session.GetString(PendingMfaUserIdKey);
        var userNameValue = HttpContext.Session.GetString(PendingMfaUserNameKey);
        var rememberMeValue = HttpContext.Session.GetString(PendingMfaRememberMeKey);
        var returnUrlValue = HttpContext.Session.GetString(PendingMfaReturnUrlKey);
        var expiresAtValue = HttpContext.Session.GetString(PendingMfaExpiresAtKey);

        if (!Guid.TryParse(userIdValue, out userId) || string.IsNullOrWhiteSpace(userNameValue))
        {
            ClearPendingState();
            return false;
        }

        if (!DateTimeOffset.TryParse(expiresAtValue, out var expiresAt) || expiresAt <= DateTimeOffset.UtcNow)
        {
            ClearPendingState();
            return false;
        }

        userName = userNameValue;
        rememberMe = bool.TryParse(rememberMeValue, out var parsedRememberMe) && parsedRememberMe;
        returnUrl = returnUrlValue;
        return true;
    }

    private void ClearPendingState()
    {
        HttpContext.Session.Remove(PendingMfaUserIdKey);
        HttpContext.Session.Remove(PendingMfaUserNameKey);
        HttpContext.Session.Remove(PendingMfaRememberMeKey);
        HttpContext.Session.Remove(PendingMfaReturnUrlKey);
        HttpContext.Session.Remove(PendingMfaExpiresAtKey);
    }
}
