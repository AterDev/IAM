using IAMMod.Managers;
using IAMMod.Models.LoginSessionDtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Share.Constants;
using Share.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SysClaimTypes = System.Security.Claims.ClaimTypes;

namespace ApiService.Pages.Account;

public class LoginModel(UserManager userManager, SessionManager sessionManager, MfaManager mfaManager, ILogger<LoginModel> logger) : PageModel
{
    private readonly UserManager _userManager = userManager;
    private readonly SessionManager _sessionManager = sessionManager;
    private readonly MfaManager _mfaManager = mfaManager;
    private readonly ILogger<LoginModel> _logger = logger;

    private const string PendingMfaUserIdKey = "PendingMfaUserId";
    private const string PendingMfaUserNameKey = "PendingMfaUserName";
    private const string PendingMfaRememberMeKey = "PendingMfaRememberMe";
    private const string PendingMfaReturnUrlKey = "PendingMfaReturnUrl";
    private const string PendingMfaExpiresAtKey = "PendingMfaExpiresAt";

    [BindProperty]
    [Required(ErrorMessage = "请输入邮箱")]
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    [Display(Name = "邮箱")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "请输入密码")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "记住我")]
    public bool RememberMe { get; set; }

    [BindProperty(SupportsGet = true, Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    public string? ClientName { get; set; }

    public async Task OnGetAsync()
    {
        // Extract client information from return URL if it's an OAuth request
        if (!string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.Contains($"{OpenIdConnectParameterNames.ClientId}=", StringComparison.Ordinal))
        {
            try
            {
                var queryStartIndex = ReturnUrl.IndexOf('?');
                if (queryStartIndex >= 0)
                {
                    var queryString = ReturnUrl.Substring(queryStartIndex);
                    var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString);

                    if (queryParams.TryGetValue(OpenIdConnectParameterNames.ClientId, out var clientId))
                    {
                        ClientName = clientId.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse client info from return URL");
            }
        }

        await Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Attempt to authenticate user
            var user = await _userManager.ValidateCredentialsAsync(Email, Password);

            if (user == null)
            {
                _logger.LogWarning("Authentication failed for email: {Email}", Email);
                ModelState.AddModelError(string.Empty, Localizer.InvalidEmailOrPassword);
                return Page();
            }

            if (user.IsTwoFactorEnabled)
            {
                HttpContext.Session.SetString(PendingMfaUserIdKey, user.Id.ToString());
                HttpContext.Session.SetString(PendingMfaUserNameKey, user.UserName);
                HttpContext.Session.SetString(PendingMfaRememberMeKey, RememberMe.ToString());
                HttpContext.Session.SetString(PendingMfaReturnUrlKey, ReturnUrl ?? string.Empty);
                HttpContext.Session.SetString(PendingMfaExpiresAtKey, DateTimeOffset.UtcNow.AddMinutes(5).ToString("O"));

                return RedirectToPage("/Account/Mfa", new { returnUrl = ReturnUrl, rememberMe = RememberMe });
            }

            // Check if user is locked out
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("User {Email} is locked out until {LockoutEnd}", Email, user.LockoutEnd);
                ModelState.AddModelError(string.Empty, "账号已被锁定,请稍后再试");
                return Page();
            }

            _logger.LogInformation("User {Email} logged in successfully", Email);

            var sessionId = Guid.CreateVersion7().ToString();
            var sessionExpiresAt = RememberMe
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
                HttpContext.Request.Headers.UserAgent.ToString()
            );

            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(SysClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(SysClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sid, sessionId)
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(SysClaimTypes.Email, user.Email));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = RememberMe,
                ExpiresUtc = RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(2)
            };

            // Sign in the user with cookie authentication
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                authProperties
            );

            // Store user info in session for OAuth flow (as backup)
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("SessionId", sessionId);

            // Redirect to return URL or default page
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return Redirect("~/");
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Login rejected for email {Email}: {LanguageKey}", Email, ex.LanguageKey);
            ModelState.AddModelError(string.Empty, ex.LanguageKey);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email {Email}", Email);
            ModelState.AddModelError(string.Empty, "登录过程中发生错误,请稍后重试");
            return Page();
        }
    }
}
