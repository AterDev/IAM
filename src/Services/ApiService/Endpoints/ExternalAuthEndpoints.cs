using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IAMMod.Managers;
using IAMMod.Models.LoginSessionDtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;

namespace ApiService.Endpoints;

public sealed class ExternalAuthEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ExternalAuth").WithTags("ExternalAuth");
        group.MapGet("/signin-microsoft", SignInMicrosoft).WithName("ExternalAuth_SignInMicrosoft").AllowAnonymous();
        group.MapGet("/signin-google", SignInGoogle).WithName("ExternalAuth_SignInGoogle").AllowAnonymous();
        group.MapGet("/getToken", GetToken).WithName("ExternalAuth_GetToken").AllowAnonymous();
    }

    private static Task<IResult> SignInMicrosoft(
        HttpContext context,
        IAuthenticationSchemeProvider schemeProvider,
        ILogger<ExternalAuthEndpoints> logger,
        string? returnUrl = null) =>
        ChallengeExternalAsync(context, schemeProvider, logger, MicrosoftAccountDefaults.AuthenticationScheme, returnUrl);

    private static Task<IResult> SignInGoogle(
        HttpContext context,
        IAuthenticationSchemeProvider schemeProvider,
        ILogger<ExternalAuthEndpoints> logger,
        string? returnUrl = null) =>
        ChallengeExternalAsync(context, schemeProvider, logger, GoogleDefaults.AuthenticationScheme, returnUrl);

    private static async Task<IResult> ChallengeExternalAsync(
        HttpContext context,
        IAuthenticationSchemeProvider schemeProvider,
        ILogger<ExternalAuthEndpoints> logger,
        string scheme,
        string? returnUrl)
    {
        if (await schemeProvider.GetSchemeAsync(scheme) is null)
        {
            logger.LogWarning("External authentication scheme {Scheme} is not configured.", scheme);
            return TypedResults.Redirect(BuildFrontendReturnUrl(context, returnUrl, new Dictionary<string, string?>
            {
                ["status"] = "provider_not_configured",
                ["provider"] = scheme,
            }));
        }

        var callbackUrl = QueryHelpers.AddQueryString(
            "/api/ExternalAuth/getToken",
            new Dictionary<string, string?> { ["type"] = scheme, ["returnUrl"] = returnUrl });

        return TypedResults.Challenge(
            new AuthenticationProperties { RedirectUri = callbackUrl },
            [scheme]);
    }

    private static async Task<IResult> GetToken(
        HttpContext context,
        UserManager userManager,
        SessionManager sessionManager,
        ILogger<ExternalAuthEndpoints> logger,
        string type,
        string? returnUrl = null)
    {
        logger.LogInformation("{Type} login callback initiated.", type);
        var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal is null)
        {
            logger.LogWarning("External authentication failed for type: {Type}", type);
            return TypedResults.Redirect(BuildFrontendReturnUrl(context, returnUrl, new Dictionary<string, string?>
            {
                ["status"] = "failed",
                ["provider"] = type,
            }));
        }

        var externalUser = result.Principal;
        var providerKey = externalUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? externalUser.FindFirst("sub")?.Value;
        var email = externalUser.FindFirst(ClaimTypes.Email)?.Value;
        var name = externalUser.FindFirst(ClaimTypes.Name)?.Value ?? email ?? providerKey;

        if (string.IsNullOrWhiteSpace(providerKey))
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return TypedResults.Redirect(BuildFrontendReturnUrl(context, returnUrl, new Dictionary<string, string?>
            {
                ["status"] = "invalid_external_identity",
                ["provider"] = type,
            }));
        }

        var resolution = await userManager.ResolveExternalLoginAsync(
            type,
            providerKey,
            email,
            name,
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers.UserAgent.ToString());

        if (resolution.Status != "success" || !resolution.UserId.HasValue || string.IsNullOrWhiteSpace(resolution.UserName))
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return TypedResults.Redirect(BuildFrontendReturnUrl(context, returnUrl, new Dictionary<string, string?>
            {
                ["status"] = resolution.Status,
                ["provider"] = type,
            }));
        }

        await SignInLocalUserAsync(context, sessionManager, resolution.UserId.Value, resolution.UserName, resolution.Email);
        return TypedResults.Redirect(BuildFrontendReturnUrl(context, returnUrl, new Dictionary<string, string?>
        {
            ["status"] = "success",
            ["provider"] = type,
            ["isNewUser"] = resolution.IsNewUser ? bool.TrueString.ToLowerInvariant() : null,
        }));
    }

    private static async Task SignInLocalUserAsync(
        HttpContext context,
        SessionManager sessionManager,
        Guid userId,
        string userName,
        string? email)
    {
        var sessionId = Guid.CreateVersion7().ToString();
        var expiration = DateTimeOffset.UtcNow.AddHours(2);
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();
        await sessionManager.AddAsync(new LoginSessionAddDto
        {
            UserId = userId,
            SessionId = sessionId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceInfo = userAgent,
            ExpirationTime = expiration,
        }, ipAddress, userAgent);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Sid, sessionId),
        };
        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = expiration });

    }

    private static string BuildFrontendReturnUrl(
        HttpContext context,
        string? returnUrl,
        IDictionary<string, string?> parameters)
    {
        var fallbackPath = "/";
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return QueryHelpers.AddQueryString(fallbackPath, parameters);
        }
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var absoluteUri))
        {
            return returnUrl.StartsWith('/')
                ? QueryHelpers.AddQueryString(returnUrl, parameters)
                : QueryHelpers.AddQueryString(fallbackPath, parameters);
        }
        if (!IsAllowedFrontChannelUri(context, absoluteUri))
        {
            return QueryHelpers.AddQueryString(fallbackPath, parameters);
        }
        return QueryHelpers.AddQueryString(returnUrl, parameters);
    }

    private static bool IsAllowedFrontChannelUri(HttpContext context, Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        var requestHost = context.Request.Host.Host;
        if (uri.Host.Equals(requestHost, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            && (uri.Port is 4200 or 4201 || uri.Port == context.Request.Host.Port);
    }
}
