using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiService.Models;
using IAMMod.Managers;
using IAMMod.Models.OAuthDtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using Share.Exceptions;

namespace ApiService.Endpoints;

public sealed class OAuthEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/connect").WithTags("OAuth");
        group.MapGet("/authorize", Authorize).WithName("OAuth_Authorize").AllowAnonymous();
        group.MapPost("/token", Token).WithName("OAuth_Token").AllowAnonymous().RequireRateLimiting(WebConst.TokenEndpoint).Accepts<TokenRequestDto>("application/x-www-form-urlencoded");
        group.MapPost("/device", Device).WithName("OAuth_DeviceAuthorization").AllowAnonymous().RequireRateLimiting(WebConst.DeviceEndpoint).Accepts<DeviceAuthorizationRequestDto>("application/x-www-form-urlencoded");
        group.MapPost("/introspect", Introspect).WithName("OAuth_Introspect").AllowAnonymous().RequireRateLimiting(WebConst.TokenEndpoint).Accepts<IntrospectRequestDto>("application/x-www-form-urlencoded");
        group.MapPost("/revoke", Revoke).WithName("OAuth_Revoke").AllowAnonymous().RequireRateLimiting(WebConst.TokenEndpoint).Accepts<RevokeRequestDto>("application/x-www-form-urlencoded");
        group.MapGet("/logout", Logout).WithName("OAuth_Logout").AllowAnonymous();
        group.MapGet("/userinfo", UserInfo).WithName("OAuth_UserInfo").RequireAuthorization();
    }

    private static async Task<IResult> Authorize(HttpContext context, [AsParameters] AuthorizeRequestDto request, AuthorizationManager authorization, ConsentManager consent, ILogger<OAuthEndpoints> logger)
    {
        try
        {
            var (valid, error, client) = await authorization.ValidateAuthorizationRequestAsync(request);
            if (!valid)
            {
                var response = new AuthorizeResponseDto { Error = error, ErrorDescription = $"Authorization request validation failed: {error}", State = request.State };
                var registeredRedirectUri = client is not null
                    && !string.IsNullOrWhiteSpace(request.RedirectUri)
                    && client.RedirectUris.Contains(request.RedirectUri);
                return registeredRedirectUri
                    ? TypedResults.Redirect(BuildRedirectUri(request.RedirectUri, response))
                    : TypedResults.BadRequest(response);
            }
            var auth = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!auth.Succeeded || auth.Principal is null) return LoginRedirect(context);
            var userId = auth.Principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sessionId = auth.Principal.FindFirst(JwtRegisteredClaimNames.Sid)?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRedirect(context);
            if (!await consent.HasValidConsentAsync(userId, client!.Id, request.Scope ?? string.Empty) && !string.Equals(context.Request.Query["consent_granted"], "true", StringComparison.OrdinalIgnoreCase))
                return TypedResults.Redirect($"/Account/Consent{context.Request.QueryString}");
            if (request.ResponseType != "code") return TypedResults.Redirect(BuildRedirectUri(request.RedirectUri, new AuthorizeResponseDto { Error = ErrorCodes.UnsupportedResponseType, ErrorDescription = "Only authorization code flow is currently supported", State = request.State }));
            var code = await authorization.CreateAuthorizationCodeAsync(userId, client.Id, request.RedirectUri, request.Scope, request.CodeChallenge, request.CodeChallengeMethod, request.Nonce, sessionId);
            return TypedResults.Redirect(BuildRedirectUri(request.RedirectUri, new AuthorizeResponseDto { Code = code, State = request.State }));
        }
        catch (Exception ex) { logger.LogError(ex, "Error processing authorization request"); return TypedResults.Problem("An error occurred processing the request", statusCode: 500); }
    }

    private static async Task<IResult> Token(HttpContext context, SigningKeyManager keys, TokenManager manager, Localizer localizer, ILogger<OAuthEndpoints> logger)
    {
        try
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            var request = new TokenRequestDto { GrantType = form["grant_type"].ToString(), ClientId = form["client_id"], ClientSecret = form["client_secret"], Code = form["code"], RedirectUri = form["redirect_uri"], CodeVerifier = form["code_verifier"], RefreshToken = form["refresh_token"], Scope = form["scope"], Username = form["username"], Password = form["password"], DeviceCode = form["device_code"], Resource = form["resource"], Audience = form["audience"] };
            var signingKey = await keys.GetActiveSigningKeyAsync();
            if (signingKey is null) return TypedResults.Problem(localizer.Get(Localizer.BadRequest), statusCode: 400);
            return TypedResults.Ok(await manager.ProcessTokenRequestAsync(request, signingKey));
        }
        catch (BusinessException ex) { logger.LogWarning(ex, "Token request rejected"); return TypedResults.Json(new OAuthErrorDto { Error = ex.LanguageKey, ErrorDescription = localizer.Get(ex.LanguageKey) ?? ex.LanguageKey }, ApiJsonSerializerContext.Default.OAuthErrorDto, statusCode: ex.StatusCodes == 500 ? 400 : ex.StatusCodes); }
        catch (Exception ex) { logger.LogError(ex, "Error processing token request"); return TypedResults.Problem("An error occurred processing the token request", statusCode: 500); }
    }

    private static async Task<IResult> Device(HttpContext context, DeviceFlowManager manager, ILogger<OAuthEndpoints> logger)
    {
        try { var form = await context.Request.ReadFormAsync(context.RequestAborted); var response = await manager.InitiateDeviceAuthorizationAsync(new DeviceAuthorizationRequestDto { ClientId = form["client_id"].ToString(), Scope = form["scope"] }); return response is null ? TypedResults.BadRequest() : TypedResults.Ok(response); }
        catch (Exception ex) { logger.LogError(ex, "Error processing device authorization request"); return TypedResults.Problem(statusCode: 500); }
    }

    private static async Task<IResult> Introspect(HttpContext context, TokenManager manager, ILogger<OAuthEndpoints> logger)
    {
        try { var form = await context.Request.ReadFormAsync(context.RequestAborted); var request = new IntrospectRequestDto { Token = form["token"].ToString(), TokenTypeHint = form["token_type_hint"], ClientId = form["client_id"], ClientSecret = form["client_secret"] }; var (id, secret) = ResolveCredentials(context, request.ClientId, request.ClientSecret); var client = await manager.ValidateSensitiveEndpointClientAsync(id, secret); if (client is null) return TypedResults.Json(new OAuthErrorDto { Error = ErrorCodes.InvalidClient, ErrorDescription = "Client authentication failed." }, ApiJsonSerializerContext.Default.OAuthErrorDto, statusCode: 401); return TypedResults.Ok(await manager.IntrospectTokenAsync(request.Token, request.TokenTypeHint, client.Id)); }
        catch (Exception ex) { logger.LogError(ex, "Error introspecting token"); return TypedResults.Problem(statusCode: 500); }
    }

    private static async Task<IResult> Revoke(HttpContext context, TokenManager manager, ILogger<OAuthEndpoints> logger)
    {
        try { var form = await context.Request.ReadFormAsync(context.RequestAborted); var request = new RevokeRequestDto { Token = form["token"].ToString(), TokenTypeHint = form["token_type_hint"], ClientId = form["client_id"], ClientSecret = form["client_secret"] }; var (id, secret) = ResolveCredentials(context, request.ClientId, request.ClientSecret); var client = await manager.ValidateSensitiveEndpointClientAsync(id, secret); if (client is null) return TypedResults.Json(new OAuthErrorDto { Error = ErrorCodes.InvalidClient, ErrorDescription = "Client authentication failed." }, ApiJsonSerializerContext.Default.OAuthErrorDto, statusCode: 401); await manager.RevokeTokenAsync(request.Token, request.TokenTypeHint, client.Id); return TypedResults.Ok(); }
        catch (Exception ex) { logger.LogError(ex, "Error revoking token"); return TypedResults.Problem(statusCode: 500); }
    }

    private static async Task<IResult> Logout(HttpContext context, LogoutRequestDto request, SessionManager sessions, ClientManager clients, ILogger<OAuthEndpoints> logger)
    {
        try { var sid = context.User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value; var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value; if (!string.IsNullOrWhiteSpace(sid) && Guid.TryParse(claim, out var userId)) { var session = await sessions.GetBySessionIdAsync(sid); if (session is not null) await sessions.RevokeSessionAsync(session.Id, userId.ToString(), context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString()); } await context.SignOutAsync(); if (Uri.TryCreate(request.PostLogoutRedirectUri, UriKind.Absolute, out var redirect)
                && (redirect.Scheme == Uri.UriSchemeHttps || redirect.Scheme == Uri.UriSchemeHttp))
            {
                var redirectUri = request.PostLogoutRedirectUri!;
                var client = await clients.FindEntityAsync(item => Enumerable.Contains(item.PostLogoutRedirectUriValues, redirectUri), context.RequestAborted);
                if (client is not null)
                {
                    return TypedResults.Redirect(string.IsNullOrWhiteSpace(request.State)
                        ? redirectUri
                        : Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(redirectUri, "state", request.State));
                }
            } return TypedResults.Ok(new ApiMessageDto("Logged out successfully")); }
        catch (Exception ex) { logger.LogError(ex, "Error processing logout request"); return TypedResults.Problem(statusCode: 500); }
    }

    private static async Task<IResult> UserInfo(HttpContext context, DiscoveryManager discovery, ILogger<OAuthEndpoints> logger)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier) ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (claim is null || !Guid.TryParse(claim.Value, out var userId)) return TypedResults.Json(new OAuthErrorDto { Error = "invalid_token", ErrorDescription = "The access token is invalid or does not contain a valid subject" }, ApiJsonSerializerContext.Default.OAuthErrorDto, statusCode: 401);
        try { var scopes = context.User.FindFirst("scope")?.Value?.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList() ?? []; var result = await discovery.GetUserInfoAsync(userId, scopes); return result is null ? TypedResults.Json(new OAuthErrorDto { Error = "user_not_found", ErrorDescription = "The user associated with this token was not found" }, ApiJsonSerializerContext.Default.OAuthErrorDto, statusCode: 404) : TypedResults.Ok(result); }
        catch (Exception ex) { logger.LogError(ex, "Failed to get user info"); return TypedResults.Problem("Failed to retrieve user information", statusCode: 500); }
    }

    private static IResult LoginRedirect(HttpContext context) => TypedResults.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
    private static string BuildRedirectUri(string baseUri, AuthorizeResponseDto response) { var p = new List<string>(); if (!string.IsNullOrEmpty(response.Code)) p.Add($"code={Uri.EscapeDataString(response.Code)}"); if (!string.IsNullOrEmpty(response.State)) p.Add($"state={Uri.EscapeDataString(response.State)}"); if (!string.IsNullOrEmpty(response.Error)) { p.Add($"error={Uri.EscapeDataString(response.Error)}"); if (!string.IsNullOrEmpty(response.ErrorDescription)) p.Add($"error_description={Uri.EscapeDataString(response.ErrorDescription)}"); } return baseUri + (baseUri.Contains('?') ? "&" : "?") + string.Join('&', p); }
    private static (string? id, string? secret) ResolveCredentials(HttpContext context, string? id, string? secret) { if (!string.IsNullOrWhiteSpace(id)) return (id, secret); if (!context.Request.Headers.TryGetValue(HeaderNames.Authorization, out var header) || !header.ToString().StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return (null, null); try { var raw = Encoding.UTF8.GetString(Convert.FromBase64String(header.ToString()[6..].Trim())); var i = raw.IndexOf(':'); return i < 0 ? (null, null) : (raw[..i], raw[(i + 1)..]); } catch (FormatException) { return (null, null); } }
}
