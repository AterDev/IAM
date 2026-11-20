using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using IAMMod.Managers;
using IAMMod.Models.LoginSessionDtos;
using IAMMod.Models.OAuthDtos;
using IAMMod.Models.UserDtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Share.Exceptions;
using SysClaimTypes = System.Security.Claims.ClaimTypes;

namespace ApiService.Browser;

public static class BrowserEndpoints
{
    private const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    public static IEndpointRouteBuilder MapBrowserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var browser = endpoints.MapGroup("").ExcludeFromDescription();
        browser.MapGet("/Account/Login", LoginGet);
        browser.MapPost("/Account/Login", LoginPostAsync);
        browser.MapGet("/Account/Mfa", MfaGet);
        browser.MapPost("/Account/Mfa", MfaPostAsync);
        browser.MapGet("/Account/Consent", ConsentGetAsync);
        browser.MapPost("/Account/Consent", ConsentPostAsync);
        browser.MapGet("/Account/Logout", LogoutGetAsync);
        browser.MapPost("/Account/Logout", LogoutPostAsync);
        browser.MapGet("/Account/LogoutSuccess", () => Results.Content(BrowserHtml.Success("您已安全登出。"), "text/html; charset=utf-8"));
        return endpoints;
    }

    private static IResult LoginGet(HttpContext context, IAntiforgery antiforgery)
    {
        var token = antiforgery.GetAndStoreTokens(context).RequestToken ?? string.Empty;
        var returnUrl = context.Request.Query["returnUrl"].ToString();
        var clientName = TryGetClientName(returnUrl);
        return Html(BrowserHtml.Login(token, returnUrl, clientName, string.Empty, false, []));
    }

    private static async Task<IResult> LoginPostAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        BrowserFlowState flowState,
        UserManager userManager,
        SessionManager sessionManager,
        MfaManager mfaManager,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!await ValidateAntiforgeryAsync(context, antiforgery))
        {
            return Html(BrowserHtml.Error("请求验证失败，请刷新页面后重试。"), StatusCodes.Status400BadRequest);
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var email = form["email"].ToString().Trim();
        var password = form["password"].ToString();
        var rememberMe = IsTrue(form["rememberMe"].ToString());
        var returnUrl = form["returnUrl"].ToString();
        var logger = loggerFactory.CreateLogger("ApiService.Browser.Login");
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("请输入邮箱");
        }
        else if (!IsEmail(email))
        {
            errors.Add("请输入有效的邮箱地址");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("请输入密码");
        }

        if (errors.Count > 0)
        {
            return LoginError(context, antiforgery, returnUrl, email, rememberMe, errors);
        }

        try
        {
            var user = await userManager.ValidateCredentialsAsync(email, password);
            if (user is null)
            {
                logger.LogWarning("Authentication failed for email {Email}", email);
                return LoginError(context, antiforgery, returnUrl, email, rememberMe, [Localizer.InvalidEmailOrPassword]);
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                return LoginError(context, antiforgery, returnUrl, email, rememberMe, ["账号已被锁定,请稍后再试"]);
            }

            if (user.IsTwoFactorEnabled)
            {
                var pending = new PendingMfaState(user.Id, user.UserName, rememberMe, LocalReturnUrl(returnUrl), DateTimeOffset.UtcNow.AddMinutes(5));
                SetPendingMfaCookie(context, flowState.Protect(pending));
                return Results.Redirect("/Account/Mfa");
            }

            var sessionId = Guid.CreateVersion7().ToString();
            var expiresAt = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(2);
            await sessionManager.AddAsync(CreateSession(user.Id, sessionId, expiresAt, context), context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString());
            await SignInAsync(context, user, sessionId, rememberMe, expiresAt);
            return Results.Redirect(LocalReturnUrl(returnUrl) ?? "/");
        }
        catch (BusinessException exception)
        {
            logger.LogWarning(exception, "Login rejected for email {Email}", email);
            return LoginError(context, antiforgery, returnUrl, email, rememberMe, [exception.LanguageKey]);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Login failed for email {Email}", email);
            return LoginError(context, antiforgery, returnUrl, email, rememberMe, ["登录过程中发生错误,请稍后重试"]);
        }
    }

    private static IResult MfaGet(HttpContext context, IAntiforgery antiforgery, BrowserFlowState flowState)
    {
        if (!TryGetPendingMfa(context, flowState, out var state))
        {
            return Results.Redirect(LoginUrl(context.Request.Query["returnUrl"].ToString()));
        }

        var token = antiforgery.GetAndStoreTokens(context).RequestToken ?? string.Empty;
        return Html(BrowserHtml.Mfa(token, state!, string.Empty, []));
    }

    private static async Task<IResult> MfaPostAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        BrowserFlowState flowState,
        UserManager userManager,
        SessionManager sessionManager,
        MfaManager mfaManager,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!await ValidateAntiforgeryAsync(context, antiforgery))
        {
            return Html(BrowserHtml.Error("请求验证失败，请刷新页面后重试。"), StatusCodes.Status400BadRequest);
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var code = form["code"].ToString().Trim();
        if (!TryGetPendingMfa(context, flowState, out var state))
        {
            return Results.Redirect(LoginUrl(null));
        }

        var token = antiforgery.GetAndStoreTokens(context).RequestToken ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            return Html(BrowserHtml.Mfa(token, state!, code, ["请输入验证码或恢复码"]));
        }

        var logger = loggerFactory.CreateLogger("ApiService.Browser.Mfa");
        try
        {
            if (!await mfaManager.VerifyLoginChallengeAsync(state!.UserId, code))
            {
                return Html(BrowserHtml.Mfa(token, state, code, ["验证码或恢复码无效"]));
            }

            if (!flowState.TryConsume(context.Request.Cookies[BrowserFlowState.PendingMfaCookieName], out var consumedState))
            {
                DeletePendingMfaCookie(context);
                return Html(BrowserHtml.Error("登录验证已完成或已失效，请重新登录。"), StatusCodes.Status400BadRequest);
            }

            state = consumedState;
            var user = await userManager.GetDetailAsync(state.UserId);
            if (user is null)
            {
                return Html(BrowserHtml.Mfa(token, state, code, ["用户不存在"]));
            }

            var sessionId = Guid.CreateVersion7().ToString();
            await sessionManager.AddAsync(CreateSession(user.Id, sessionId, state.ExpiresAt, context), context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString());
            await SignInAsync(context, user, sessionId, state.RememberMe, state.ExpiresAt);
            DeletePendingMfaCookie(context);
            return Results.Redirect(state.ReturnUrl ?? "/");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "MFA verification failed for user {UserId}", state!.UserId);
            return Html(BrowserHtml.Mfa(token, state, code, ["验证过程中发生错误，请稍后重试"]));
        }
    }

    private static async Task<IResult> ConsentGetAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        ClientManager clientManager,
        ScopeManager scopeManager,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var principal = await CookiePrincipalAsync(context);
        if (principal is null)
        {
            return Results.Redirect(LoginUrl(context.Request.Path + context.Request.QueryString));
        }

        var form = ReadConsentForm(context.Request.Query, false);
        var userName = principal.FindFirst(SysClaimTypes.Name)?.Value ?? "Unknown User";
        var token = antiforgery.GetAndStoreTokens(context).RequestToken ?? string.Empty;
        var scopes = await LoadScopesAsync(form.Scope, scopeManager, loggerFactory.CreateLogger("ApiService.Browser.Consent"), cancellationToken);
        var clientName = form.ClientId;
        string? description = null;
        try
        {
            var client = await clientManager.FindEntityAsync(clientItem => clientItem.ClientIdentifier == form.ClientId);
            if (client is not null)
            {
                clientName = client.DisplayName;
                description = client.Description;
            }
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("ApiService.Browser.Consent").LogWarning(exception, "Failed to load client {ClientId}", form.ClientId);
        }

        return Html(BrowserHtml.Consent(token, clientName, description, userName, scopes, form, []));
    }

    private static async Task<IResult> ConsentPostAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        AuthorizationManager authorizationManager,
        ConsentManager consentManager,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!await ValidateAntiforgeryAsync(context, antiforgery))
        {
            return Html(BrowserHtml.Error("请求验证失败，请刷新页面后重试。"), StatusCodes.Status400BadRequest);
        }

        var principal = await CookiePrincipalAsync(context);
        if (principal is null)
        {
            return Results.Redirect(LoginUrl(null));
        }

        var formValues = await context.Request.ReadFormAsync(cancellationToken);
        var form = ReadConsentForm(formValues, IsTrue(formValues["rememberConsent"].ToString()));
        var userId = principal.FindFirst(SysClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Redirect(LoginUrl(null));
        }


        try
        {
            var (valid, _, client) = await authorizationManager.ValidateAuthorizationRequestAsync(new AuthorizeRequestDto
            {
                ClientId = form.ClientId,
                RedirectUri = form.RedirectUri ?? string.Empty,
                ResponseType = form.ResponseType ?? string.Empty,
                Scope = form.Scope,
                CodeChallenge = form.CodeChallenge,
                CodeChallengeMethod = form.CodeChallengeMethod,
                State = form.State,
                Nonce = form.Nonce,
            });
            if (!valid || client is null)
            {
                return Html(BrowserHtml.Error("客户端或回调地址无效。"), StatusCodes.Status400BadRequest);
            }

            if (string.Equals(formValues["action"], "deny", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Redirect(BuildDeniedRedirect(form.RedirectUri, form.State));
            }

            await consentManager.GrantConsentAsync(userId, client.Id, form.Scope, form.RememberConsent);
            return Results.Redirect(BuildAuthorizeUrl(form));
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("ApiService.Browser.Consent").LogError(exception, "Error processing consent");
            return Html(BrowserHtml.Error("授权过程中发生错误，请稍后重试。"), StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> LogoutGetAsync(HttpContext context, IAntiforgery antiforgery, ClientManager clientManager)
    {
        var principal = await CookiePrincipalAsync(context);
        var token = antiforgery.GetAndStoreTokens(context).RequestToken ?? string.Empty;
        var form = ReadLogoutForm(context.Request.Query);
        var redirect = await GetLogoutRedirectAsync(clientManager, form, context.RequestAborted);
        form = form with { PostLogoutRedirectUri = redirect is null ? null : form.PostLogoutRedirectUri };
        return Html(BrowserHtml.Logout(token, principal?.FindFirst(SysClaimTypes.Name)?.Value, form));
    }

    private static async Task<IResult> LogoutPostAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        ClientManager clientManager,
        SessionManager sessionManager,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!await ValidateAntiforgeryAsync(context, antiforgery))
        {
            return Html(BrowserHtml.Error("请求验证失败，请刷新页面后重试。"), StatusCodes.Status400BadRequest);
        }

        var formValues = await context.Request.ReadFormAsync(cancellationToken);
        var principal = await CookiePrincipalAsync(context);
        var form = ReadLogoutForm(formValues);
        try
        {
            var sid = principal?.FindFirst(JwtRegisteredClaimNames.Sid)?.Value;
            var userIdValue = principal?.FindFirst(SysClaimTypes.NameIdentifier)?.Value ?? principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!string.IsNullOrWhiteSpace(sid) && Guid.TryParse(userIdValue, out var userId))
            {
                var session = await sessionManager.GetBySessionIdAsync(sid);
                if (session is not null)
                {
                    await sessionManager.RevokeSessionAsync(session.Id, userId.ToString(), context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString());
                }
            }

            await context.SignOutAsync(CookieScheme);
            var redirect = await GetLogoutRedirectAsync(clientManager, form, cancellationToken);
            return string.IsNullOrWhiteSpace(redirect)
                ? Html(BrowserHtml.Success("您已安全登出。"))
                : Results.Redirect(redirect);
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("ApiService.Browser.Logout").LogError(exception, "Error during logout");
            return Html(BrowserHtml.Error("登出过程中发生错误，请稍后重试。"), StatusCodes.Status500InternalServerError);
        }
    }

    private static IResult LoginError(HttpContext context, IAntiforgery antiforgery, string? returnUrl, string email, bool rememberMe, IReadOnlyList<string> errors)
    {
        var token = antiforgery.GetAndStoreTokens(context).RequestToken ?? string.Empty;
        return Html(BrowserHtml.Login(token, returnUrl, TryGetClientName(returnUrl), email, rememberMe, errors), StatusCodes.Status400BadRequest);
    }

    private static async Task<bool> ValidateAntiforgeryAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    private static async Task<ClaimsPrincipal?> CookiePrincipalAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync(CookieScheme);
        return result.Succeeded ? result.Principal : null;
    }

    private static async Task SignInAsync(HttpContext context, UserDetailDto user, string sessionId, bool rememberMe, DateTimeOffset expiresAt)
    {
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

        await context.SignInAsync(CookieScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieScheme)), new AuthenticationProperties { IsPersistent = rememberMe, ExpiresUtc = expiresAt });
    }

    private static LoginSessionAddDto CreateSession(Guid userId, string sessionId, DateTimeOffset expiresAt, HttpContext context) => new()
    {
        UserId = userId,
        SessionId = sessionId,
        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
        UserAgent = context.Request.Headers.UserAgent.ToString(),
        DeviceInfo = context.Request.Headers.UserAgent.ToString(),
        ExpirationTime = expiresAt,
    };

    private static bool TryGetPendingMfa(HttpContext context, BrowserFlowState flowState, out PendingMfaState? state)
    {
        var found = flowState.TryUnprotect(context.Request.Cookies[BrowserFlowState.PendingMfaCookieName], out state);
        if (!found)
        {
            DeletePendingMfaCookie(context);
        }

        return found;
    }

    private static void SetPendingMfaCookie(HttpContext context, string value) => context.Response.Cookies.Append(BrowserFlowState.PendingMfaCookieName, value, new CookieOptions { HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax, Secure = context.Request.IsHttps, MaxAge = TimeSpan.FromMinutes(5), Path = "/" });

    private static void DeletePendingMfaCookie(HttpContext context) => context.Response.Cookies.Delete(BrowserFlowState.PendingMfaCookieName, new CookieOptions { Path = "/" });

    private static string? LocalReturnUrl(string? value) => IsLocalUrl(value) ? value : null;

    private static bool IsLocalUrl(string? value) => !string.IsNullOrWhiteSpace(value) && value.StartsWith('/') && !value.StartsWith("//") && !value.StartsWith("/\\");

    private static string LoginUrl(string? returnUrl) => string.IsNullOrWhiteSpace(returnUrl) ? "/Account/Login" : Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("/Account/Login", "returnUrl", returnUrl);

    private static string? TryGetClientName(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        var queryIndex = returnUrl.IndexOf('?');
        if (queryIndex < 0)
        {
            return null;
        }

        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(returnUrl[queryIndex..]);
        return query.TryGetValue(OpenIdConnectParameterNames.ClientId, out var clientId) ? clientId.ToString() : null;
    }

    private static ConsentForm ReadConsentForm(IEnumerable<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> values, bool rememberConsent)
    {
        var query = values.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        return new ConsentForm(
            Get(query, "client_id") ?? string.Empty,
            Get(query, "scope") ?? string.Empty,
            Get(query, "state"), Get(query, "nonce"), Get(query, "code_challenge"), Get(query, "code_challenge_method"), Get(query, "redirect_uri"), Get(query, "response_type"), rememberConsent);
    }

    private static LogoutForm ReadLogoutForm(IEnumerable<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> values)
    {
        var query = values.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        return new LogoutForm(Get(query, "post_logout_redirect_uri"), Get(query, "state"), Get(query, "id_token_hint"));
    }

    private static string? Get(IReadOnlyDictionary<string, string> values, string key) => values.TryGetValue(key, out var value) ? value : null;

    private static async Task<List<ScopeViewModel>> LoadScopesAsync(string scopeValue, ScopeManager scopeManager, ILogger logger, CancellationToken cancellationToken)
    {
        var scopes = new List<ScopeViewModel>();
        foreach (var scopeName in scopeValue.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var scope = await scopeManager.FindEntityAsync(item => item.Name == scopeName);
                scopes.Add(new ScopeViewModel(scopeName, scope?.DisplayName ?? scopeName, scope?.Description ?? DefaultScopeDescription(scopeName), scope?.Required ?? scopeName == OpenIdConnectScope.OpenId));
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to load scope {Scope}", scopeName);
                scopes.Add(new ScopeViewModel(scopeName, scopeName, DefaultScopeDescription(scopeName), scopeName == OpenIdConnectScope.OpenId));
            }
        }

        return scopes;
    }

    private static string DefaultScopeDescription(string scopeName) => scopeName switch
    {
        OpenIdConnectScope.OpenId => "您的基本身份标识",
        "profile" => "您的基本个人信息（姓名等）",
        OpenIdConnectScope.Email => "您的电子邮箱地址",
        OpenIdConnectScope.Phone => "您的电话号码",
        OpenIdConnectScope.Address => "您的地址信息",
        OpenIdConnectScope.OfflineAccess => "在您离线时访问您的数据",
        _ => $"访问 {scopeName} 资源的权限",
    };

    private static string BuildAuthorizeUrl(ConsentForm form)
    {
        var values = new Dictionary<string, string?>
        {
            [OpenIdConnectParameterNames.ClientId] = form.ClientId,
            [OpenIdConnectParameterNames.Scope] = form.Scope,
            [OpenIdConnectParameterNames.ResponseType] = form.ResponseType,
            [OpenIdConnectParameterNames.RedirectUri] = form.RedirectUri,
            [OpenIdConnectParameterNames.State] = form.State,
            [OpenIdConnectParameterNames.Nonce] = form.Nonce,
            ["code_challenge"] = form.CodeChallenge,
            ["code_challenge_method"] = form.CodeChallengeMethod,
            ["consent_granted"] = "true",
        };
        return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("/connect/authorize", values);
    }

    private static string BuildDeniedRedirect(string? redirectUri, string? state)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return "/Account/Consent";
        }

        return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(redirectUri, new Dictionary<string, string?> { ["error"] = "access_denied", ["error_description"] = "User denied authorization", ["state"] = state });
    }

    private static async Task<string?> GetLogoutRedirectAsync(ClientManager clientManager, LogoutForm form, CancellationToken cancellationToken)
    {
        var redirectUri = form.PostLogoutRedirectUri;
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            return null;
        }

        var client = await clientManager.FindEntityAsync(item => item.PostLogoutRedirectUriValues.Contains(redirectUri!), cancellationToken);
        if (client is null || !client.PostLogoutRedirectUris.Contains(redirectUri!, StringComparer.Ordinal))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(form.State)
            ? redirectUri
            : Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(redirectUri!, "state", form.State);
    }

    private static bool IsTrue(string value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1" || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);

    private static bool IsEmail(string value)
    {
        try
        {
            return new System.Net.Mail.MailAddress(value).Address.Equals(value, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static IResult Html(string content, int statusCode = StatusCodes.Status200OK) => Results.Content(content, "text/html; charset=utf-8", System.Text.Encoding.UTF8, statusCode);
}
