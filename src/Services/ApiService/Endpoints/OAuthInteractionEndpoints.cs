using Perigon.PostgreSQL;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IAMMod.Managers;
using IAMMod.Models.OAuthDtos;
using EntityFramework.AppDbContext;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ApiService.Endpoints;

public sealed class OAuthInteractionEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/connect/interaction").WithTags("OAuth interaction");
        group.MapGet("/authorize", GetAuthorize).WithName("OAuthInteraction_GetAuthorizeInteraction").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme });
        group.MapPost("/authorize/decision", SubmitAuthorize).WithName("OAuthInteraction_SubmitAuthorizeDecision").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme });
        group.MapGet("/device", GetDevice).WithName("OAuthInteraction_GetDeviceInteraction").AllowAnonymous().RequireRateLimiting(WebConst.DeviceEndpoint);
        group.MapPost("/device/decision", SubmitDevice).WithName("OAuthInteraction_SubmitDeviceDecision").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme }).RequireRateLimiting(WebConst.DeviceEndpoint);
    }

    private static async Task<ApiResult<AuthorizeInteractionContextDto>> GetAuthorize([AsParameters] AuthorizeRequestDto request, AuthorizationManager authorization, ConsentManager consent, DefaultDbContext db, HttpContext context, ILogger<OAuthInteractionEndpoints> logger)
    {
        var userId = UserId(context); if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Unauthorized();
        try { var (valid, error, client) = await authorization.ValidateAuthorizationRequestAsync(request); if (!valid || client is null) return BadRequest(error ?? ErrorCodes.InvalidRequest); var requested = await BuildScopes(request.Scope, db); return new AuthorizeInteractionContextDto { ClientId = client.ClientId, ClientName = client.DisplayName ?? client.ClientId, ClientDescription = client.Description, Scope = request.Scope, RequestedScopes = requested, RedirectUri = request.RedirectUri, ResponseType = request.ResponseType, State = request.State, Nonce = request.Nonce, CodeChallenge = request.CodeChallenge, CodeChallengeMethod = request.CodeChallengeMethod, ResponseMode = request.ResponseMode, UserName = context.User.Identity?.Name, HasValidConsent = await consent.HasValidConsentAsync(userId, client.Id, request.Scope ?? string.Empty) }; }
        catch (Exception ex) { logger.LogError(ex, "Failed to load authorize interaction context"); return Problem(); }
    }

    private static async Task<ApiResult<AuthorizeInteractionDecisionResponseDto>> SubmitAuthorize(AuthorizeInteractionDecisionDto request, AuthorizationManager authorization, ConsentManager consent, HttpContext context, ILogger<OAuthInteractionEndpoints> logger)
    {
        var userId = UserId(context); if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Unauthorized();
        try { var auth = new AuthorizeRequestDto { ClientId = request.ClientId, RedirectUri = request.RedirectUri, ResponseType = request.ResponseType, Scope = request.Scope, State = request.State, Nonce = request.Nonce, CodeChallenge = request.CodeChallenge, CodeChallengeMethod = request.CodeChallengeMethod, ResponseMode = request.ResponseMode }; var (valid, error, client) = await authorization.ValidateAuthorizationRequestAsync(auth); if (!valid || client is null) return BadRequest(error ?? ErrorCodes.InvalidRequest); if (!request.Approve) return new AuthorizeInteractionDecisionResponseDto { Status = "denied", RedirectUrl = BuildRedirect(request.RedirectUri, new AuthorizeResponseDto { Error = ErrorCodes.AccessDenied, ErrorDescription = "User denied authorization", State = request.State }), Message = "User denied authorization" }; if (request.ResponseType != OpenIdConnectResponseType.Code) return new AuthorizeInteractionDecisionResponseDto { Status = "unsupported_response_type", RedirectUrl = BuildRedirect(request.RedirectUri, new AuthorizeResponseDto { Error = ErrorCodes.UnsupportedResponseType, ErrorDescription = "Only authorization code flow is currently supported", State = request.State }), Message = "Only authorization code flow is currently supported" }; await consent.GrantConsentAsync(userId, client.Id, request.Scope ?? string.Empty, request.RememberConsent); var code = await authorization.CreateAuthorizationCodeAsync(userId, client.Id, request.RedirectUri, request.Scope, request.CodeChallenge, request.CodeChallengeMethod, request.Nonce, context.User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value); return new AuthorizeInteractionDecisionResponseDto { Status = "approved", RedirectUrl = BuildRedirect(request.RedirectUri, new AuthorizeResponseDto { Code = code, State = request.State }) }; }
        catch (Exception ex) { logger.LogError(ex, "Failed to submit authorize interaction decision"); return Problem(); }
    }

    private static async Task<ApiResult<DeviceAuthorizationInteractionDto>> GetDevice(string userCode, DeviceFlowManager manager, ILogger<OAuthInteractionEndpoints> logger)
    { if (string.IsNullOrWhiteSpace(userCode)) return BadRequest(ErrorCodes.InvalidRequest); try { return await manager.GetDeviceAuthorizationInteractionAsync(userCode); } catch (Exception ex) { logger.LogError(ex, "Failed to load device interaction"); return Problem(); } }

    private static async Task<ApiResult<DeviceAuthorizationInteractionDto>> SubmitDevice(DeviceAuthorizationDecisionDto request, DeviceFlowManager manager, HttpContext context, ILogger<OAuthInteractionEndpoints> logger)
    { var userId = UserId(context); if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Unauthorized(); try { var current = await manager.GetDeviceAuthorizationInteractionAsync(request.UserCode); if (current.Status != "pending") return current; var success = request.Approve ? await manager.ApproveDeviceAuthorizationAsync(request.UserCode, userId) : await manager.DenyDeviceAuthorizationAsync(request.UserCode); var updated = await manager.GetDeviceAuthorizationInteractionAsync(request.UserCode); if (!success && updated.Status == "pending") { updated.Message = "Unable to process the device authorization decision."; return BadRequest(updated.Message); } return updated; } catch (Exception ex) { logger.LogError(ex, "Failed to submit device decision"); return Problem(); } }

    private static string? UserId(HttpContext context) => context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    private static string BuildRedirect(string uri, AuthorizeResponseDto response) { var p = new List<string>(); if (!string.IsNullOrEmpty(response.Code)) p.Add($"code={Uri.EscapeDataString(response.Code)}"); if (!string.IsNullOrEmpty(response.State)) p.Add($"state={Uri.EscapeDataString(response.State)}"); if (!string.IsNullOrEmpty(response.Error)) p.Add($"error={Uri.EscapeDataString(response.Error)}"); return uri + (uri.Contains('?') ? "&" : "?") + string.Join('&', p); }
    private static async Task<List<OAuthInteractionScopeDto>> BuildScopes(string? scope, DefaultDbContext db)
    {
        var names = (scope ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length == 0)
        {
            return [];
        }
        var scopes = await db.ApiScopes.Where(item => names.Contains(item.Name)).ToListAsync();
        var infoByName = scopes.ToDictionary(item => item.Name);
        return names.Select(name =>
        {
            infoByName.TryGetValue(name, out var info);
            return new OAuthInteractionScopeDto
            {
                Name = name,
                DisplayName = info?.DisplayName ?? name,
                Description = info?.Description ?? $"Access permission for {name}",
                Required = info?.Required ?? name == OpenIdConnectScope.OpenId,
            };
        }).ToList();
    }
}
