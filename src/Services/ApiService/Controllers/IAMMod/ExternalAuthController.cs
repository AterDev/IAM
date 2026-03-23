using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using IAMMod.Managers;
using IAMMod.Models.LoginSessionDtos;
using System.IdentityModel.Tokens.Jwt;
using ClaimTypes = System.Security.Claims.ClaimTypes;
using SysClaimTypes = System.Security.Claims.ClaimTypes;

namespace ApiService.Controllers.IAMMod;

[Route("api/[controller]")]
public class ExternalAuthController(
    Localizer localizer,
    UserManager userManager,
    SessionManager sessionManager,
    IAuthenticationSchemeProvider schemeProvider,
    ILogger<ExternalAuthController> logger
) : RestControllerBase(localizer)
{
    private readonly UserManager _userManager = userManager;
    private readonly SessionManager _sessionManager = sessionManager;
    private readonly IAuthenticationSchemeProvider _schemeProvider = schemeProvider;

    /// <summary>
    /// Microsft login
    /// </summary>
    /// <param name="returnUrl"></param>
    /// <returns></returns>
    [HttpGet("signin-microsoft")]
    [AllowAnonymous]
    public Task<IActionResult> SignInMicrosoft(string? returnUrl = null)
    {
        return ChallengeExternalAsync(MicrosoftAccountDefaults.AuthenticationScheme, returnUrl);
    }

    /// <summary>
    /// Google login
    /// </summary>
    /// <param name="returnUrl"></param>
    /// <returns></returns>
    [HttpGet("signin-google")]
    [AllowAnonymous]
    public Task<IActionResult> SignInGoogle(string? returnUrl = null)
    {
        return ChallengeExternalAsync(GoogleDefaults.AuthenticationScheme, returnUrl);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="type"></param>
    /// <param name="returnUrl"></param>
    /// <returns></returns>
    [HttpGet("getToken")]
    [AllowAnonymous]
    public async Task<IActionResult> GetToken(string type, string? returnUrl = null)
    {
        logger.LogInformation("{type} login callback initiated.", type);

        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal == null)
        {
            logger.LogWarning("External authentication failed for type: {type}", type);
            return Redirect(BuildFrontendReturnUrl(returnUrl, new Dictionary<string, string?>
            {
                ["status"] = "failed",
                ["provider"] = type,
            }));
        }

        var externalUser = result.Principal;
        var providerKey = externalUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? externalUser.FindFirst("sub")?.Value;
        var email = externalUser.FindFirst(ClaimTypes.Email)?.Value;
        var name = externalUser.FindFirst(ClaimTypes.Name)?.Value
            ?? email
            ?? providerKey;

        if (string.IsNullOrWhiteSpace(providerKey))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect(BuildFrontendReturnUrl(returnUrl, new Dictionary<string, string?>
            {
                ["status"] = "invalid_external_identity",
                ["provider"] = type,
            }));
        }

        var resolution = await _userManager.ResolveExternalLoginAsync(
            type,
            providerKey,
            email,
            name,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers.UserAgent.ToString());

        if (resolution.Status != "success" || !resolution.UserId.HasValue || string.IsNullOrWhiteSpace(resolution.UserName))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect(BuildFrontendReturnUrl(returnUrl, new Dictionary<string, string?>
            {
                ["status"] = resolution.Status,
                ["provider"] = type,
            }));
        }

        await SignInLocalUserAsync(resolution.UserId.Value, resolution.UserName, resolution.Email);

        return Redirect(BuildFrontendReturnUrl(returnUrl, new Dictionary<string, string?>
        {
            ["status"] = "success",
            ["provider"] = type,
            ["isNewUser"] = resolution.IsNewUser ? bool.TrueString.ToLowerInvariant() : null,
        }));
    }

    private async Task<IActionResult> ChallengeExternalAsync(string scheme, string? returnUrl)
    {
        if (await _schemeProvider.GetSchemeAsync(scheme) == null)
        {
            logger.LogWarning("External authentication scheme {Scheme} is not configured.", scheme);
            return Redirect(BuildFrontendReturnUrl(returnUrl, new Dictionary<string, string?>
            {
                ["status"] = "provider_not_configured",
                ["provider"] = scheme,
            }));
        }

        var callbackUrl = Url.Action(nameof(GetToken), new { type = scheme, returnUrl });
        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            return Redirect(BuildFrontendReturnUrl(returnUrl, new Dictionary<string, string?>
            {
                ["status"] = "failed",
                ["provider"] = scheme,
            }));
        }

        var props = new AuthenticationProperties
        {
            RedirectUri = callbackUrl,
        };

        return Challenge(props, scheme);
    }

    private async Task SignInLocalUserAsync(Guid userId, string userName, string? email)
    {
        var sessionId = Guid.CreateVersion7().ToString();
        var sessionExpiresAt = DateTimeOffset.UtcNow.AddHours(2);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        await _sessionManager.AddAsync(
            new LoginSessionAddDto
            {
                UserId = userId,
                SessionId = sessionId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceInfo = userAgent,
                ExpirationTime = sessionExpiresAt,
            },
            ipAddress,
            userAgent);

        var claims = new List<System.Security.Claims.Claim>
        {
            new(SysClaimTypes.NameIdentifier, userId.ToString()),
            new(SysClaimTypes.Name, userName),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Sid, sessionId),
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new System.Security.Claims.Claim(SysClaimTypes.Email, email));
        }

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = sessionExpiresAt,
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            authProperties);

        HttpContext.Session.SetString("UserId", userId.ToString());
        HttpContext.Session.SetString("UserName", userName);
        HttpContext.Session.SetString("SessionId", sessionId);
    }

    private string BuildFrontendReturnUrl(string? returnUrl, IDictionary<string, string?> parameters)
    {
        var fallbackPath = Url.Content("~/") ?? "/";
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return QueryHelpers.AddQueryString(fallbackPath, parameters!);
        }

        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var absoluteUri))
        {
            if (returnUrl.StartsWith('/'))
            {
                return QueryHelpers.AddQueryString(returnUrl, parameters!);
            }

            return QueryHelpers.AddQueryString(fallbackPath, parameters!);
        }

        if (!IsAllowedFrontChannelUri(absoluteUri))
        {
            logger.LogWarning("Rejected external auth return url {ReturnUrl}", returnUrl);
            return QueryHelpers.AddQueryString(fallbackPath, parameters!);
        }

        return QueryHelpers.AddQueryString(returnUrl, parameters!);
    }

    private bool IsAllowedFrontChannelUri(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var requestHost = HttpContext.Request.Host.Host;
        if (uri.Host.Equals(requestHost, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            && (uri.Port == 4200 || uri.Port == 4201 || uri.Port == HttpContext.Request.Host.Port))
        {
            return true;
        }

        return false;
    }
}
