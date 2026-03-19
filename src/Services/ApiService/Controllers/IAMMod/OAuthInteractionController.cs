using EntityFramework.AppDbContext;
using IAMMod.Managers;
using IAMMod.Models.OAuthDtos;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Share.Constants;
using System.Security.Claims;
using SysClaimTypes = System.Security.Claims.ClaimTypes;

namespace ApiService.Controllers.IAMMod;

/// <summary>
/// Interaction endpoints used by the SPA authorize and device-code pages.
/// </summary>
[Route("connect/interaction")]
public class OAuthInteractionController(
    DefaultDbContext dbContext,
    AuthorizationManager authorizationManager,
    ConsentManager consentManager,
    DeviceFlowManager deviceFlowManager,
    Localizer localizer,
    ILogger<OAuthInteractionController> logger) : RestControllerBase(localizer)
{
    private readonly DefaultDbContext _dbContext = dbContext;
    private readonly AuthorizationManager _authorizationManager = authorizationManager;
    private readonly ConsentManager _consentManager = consentManager;
    private readonly DeviceFlowManager _deviceFlowManager = deviceFlowManager;
    private readonly ILogger<OAuthInteractionController> _logger = logger;

    /// <summary>
    /// Get interaction context for the SPA authorize page.
    /// </summary>
    [HttpGet("authorize")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<AuthorizeInteractionContextDto>> GetAuthorizeInteraction([FromQuery] AuthorizeRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var (isValid, error, client) = await _authorizationManager.ValidateAuthorizationRequestAsync(request);
            if (!isValid || client == null)
            {
                return BadRequest(new { error = error ?? ErrorCodes.InvalidRequest });
            }

            var hasValidConsent = await _consentManager.HasValidConsentAsync(userId, client.Id, request.Scope ?? string.Empty);
            var requestedScopes = await BuildScopeDtosAsync(request.Scope);

            return Ok(new AuthorizeInteractionContextDto
            {
                ClientId = client.ClientId,
                ClientName = client.DisplayName ?? client.ClientId,
                ClientDescription = client.Description,
                Scope = request.Scope,
                RequestedScopes = requestedScopes,
                RedirectUri = request.RedirectUri,
                ResponseType = request.ResponseType,
                State = request.State,
                Nonce = request.Nonce,
                CodeChallenge = request.CodeChallenge,
                CodeChallengeMethod = request.CodeChallengeMethod,
                ResponseMode = request.ResponseMode,
                UserName = GetCurrentUserName(User),
                HasValidConsent = hasValidConsent,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load authorize interaction context");
            return Problem();
        }
    }

    /// <summary>
    /// Submit an allow or deny decision for the SPA authorize page.
    /// </summary>
    [HttpPost("authorize/decision")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<AuthorizeInteractionDecisionResponseDto>> SubmitAuthorizeDecision([FromBody] AuthorizeInteractionDecisionDto request)
    {
        try
        {
            var userId = GetCurrentUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var authorizeRequest = new AuthorizeRequestDto
            {
                ClientId = request.ClientId,
                RedirectUri = request.RedirectUri,
                ResponseType = request.ResponseType,
                Scope = request.Scope,
                State = request.State,
                Nonce = request.Nonce,
                CodeChallenge = request.CodeChallenge,
                CodeChallengeMethod = request.CodeChallengeMethod,
                ResponseMode = request.ResponseMode,
            };

            var (isValid, error, client) = await _authorizationManager.ValidateAuthorizationRequestAsync(authorizeRequest);
            if (!isValid || client == null)
            {
                return BadRequest(new { error = error ?? ErrorCodes.InvalidRequest });
            }

            if (!request.Approve)
            {
                return Ok(new AuthorizeInteractionDecisionResponseDto
                {
                    Status = "denied",
                    RedirectUrl = BuildRedirectUri(
                        request.RedirectUri,
                        new AuthorizeResponseDto
                        {
                            Error = ErrorCodes.AccessDenied,
                            ErrorDescription = "User denied authorization",
                            State = request.State,
                        },
                        request.ResponseMode),
                    Message = "User denied authorization",
                });
            }

            if (request.ResponseType != ResponseTypes.Code)
            {
                return Ok(new AuthorizeInteractionDecisionResponseDto
                {
                    Status = "unsupported_response_type",
                    RedirectUrl = BuildRedirectUri(
                        request.RedirectUri,
                        new AuthorizeResponseDto
                        {
                            Error = ErrorCodes.UnsupportedResponseType,
                            ErrorDescription = "Only authorization code flow is currently supported",
                            State = request.State,
                        },
                        request.ResponseMode),
                    Message = "Only authorization code flow is currently supported",
                });
            }

            await _consentManager.GrantConsentAsync(userId, client.Id, request.Scope ?? string.Empty, request.RememberConsent);

            var code = await _authorizationManager.CreateAuthorizationCodeAsync(
                userId,
                client.Id,
                request.RedirectUri,
                request.Scope,
                request.CodeChallenge,
                request.CodeChallengeMethod,
                request.Nonce,
                GetSessionId(User));

            return Ok(new AuthorizeInteractionDecisionResponseDto
            {
                Status = "approved",
                RedirectUrl = BuildRedirectUri(
                    request.RedirectUri,
                    new AuthorizeResponseDto
                    {
                        Code = code,
                        State = request.State,
                    },
                    request.ResponseMode),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit authorize interaction decision");
            return Problem();
        }
    }

    /// <summary>
    /// Get device-code interaction context by user code.
    /// </summary>
    [HttpGet("device")]
    [AllowAnonymous]
    [EnableRateLimiting(WebConst.DeviceEndpoint)]
    public async Task<ActionResult<DeviceAuthorizationInteractionDto>> GetDeviceInteraction([FromQuery] string userCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userCode))
            {
                return BadRequest(new { error = ErrorCodes.InvalidRequest, error_description = "User code is required." });
            }

            var interaction = await _deviceFlowManager.GetDeviceAuthorizationInteractionAsync(userCode);
            return Ok(interaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load device interaction for user code {UserCode}", userCode);
            return Problem();
        }
    }

    /// <summary>
    /// Submit an allow or deny decision for a device-code interaction.
    /// </summary>
    [HttpPost("device/decision")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [EnableRateLimiting(WebConst.DeviceEndpoint)]
    public async Task<ActionResult<DeviceAuthorizationInteractionDto>> SubmitDeviceDecision([FromBody] DeviceAuthorizationDecisionDto request)
    {
        try
        {
            var userId = GetCurrentUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var current = await _deviceFlowManager.GetDeviceAuthorizationInteractionAsync(request.UserCode);
            if (current.Status != "pending")
            {
                return Ok(current);
            }

            var success = request.Approve
                ? await _deviceFlowManager.ApproveDeviceAuthorizationAsync(request.UserCode, userId)
                : await _deviceFlowManager.DenyDeviceAuthorizationAsync(request.UserCode);

            var updated = await _deviceFlowManager.GetDeviceAuthorizationInteractionAsync(request.UserCode);
            if (!success && updated.Status == "pending")
            {
                updated.Message = "Unable to process the device authorization decision.";
                return BadRequest(updated);
            }

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit device interaction decision for user code {UserCode}", request.UserCode);
            return Problem();
        }
    }

    private async Task<List<OAuthInteractionScopeDto>> BuildScopeDtosAsync(string? scope)
    {
        var scopeNames = (scope ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (scopeNames.Length == 0)
        {
            return [];
        }

        var scopeMap = await _dbContext.ApiScopes
            .Where(s => scopeNames.Contains(s.Name))
            .ToDictionaryAsync(s => s.Name, StringComparer.OrdinalIgnoreCase);

        return scopeNames.Select(scopeName =>
        {
            if (scopeMap.TryGetValue(scopeName, out var scopeInfo))
            {
                return new OAuthInteractionScopeDto
                {
                    Name = scopeName,
                    DisplayName = scopeInfo.DisplayName ?? scopeName,
                    Description = scopeInfo.Description ?? GetDefaultScopeDescription(scopeName),
                    Required = scopeInfo.Required,
                };
            }

            return new OAuthInteractionScopeDto
            {
                Name = scopeName,
                DisplayName = scopeName,
                Description = GetDefaultScopeDescription(scopeName),
                Required = IsDefaultRequiredScope(scopeName),
            };
        }).ToList();
    }

    private string BuildRedirectUri(string baseUri, AuthorizeResponseDto response, string? responseMode)
    {
        var parameters = new List<string>();

        if (!string.IsNullOrEmpty(response.Code))
        {
            parameters.Add($"code={Uri.EscapeDataString(response.Code)}");
        }

        if (!string.IsNullOrEmpty(response.State))
        {
            parameters.Add($"state={Uri.EscapeDataString(response.State)}");
        }

        if (!string.IsNullOrEmpty(response.Error))
        {
            parameters.Add($"error={Uri.EscapeDataString(response.Error)}");
            if (!string.IsNullOrEmpty(response.ErrorDescription))
            {
                parameters.Add($"error_description={Uri.EscapeDataString(response.ErrorDescription)}");
            }
        }

        var separator = baseUri.Contains('?') ? "&" : "?";
        return $"{baseUri}{separator}{string.Join("&", parameters)}";
    }

    private static string? GetCurrentUserId(ClaimsPrincipal principal)
    {
        return principal.FindFirst(OAuthConst.ClaimTypes.Subject)?.Value
            ?? principal.FindFirst(SysClaimTypes.NameIdentifier)?.Value;
    }

    private static string? GetCurrentUserName(ClaimsPrincipal principal)
    {
        return principal.FindFirst(SysClaimTypes.Name)?.Value
            ?? principal.FindFirst(OAuthConst.ClaimTypes.Name)?.Value
            ?? principal.FindFirst("preferred_username")?.Value;
    }

    private static string? GetSessionId(ClaimsPrincipal principal)
    {
        return principal.FindFirst("sid")?.Value;
    }

    private static string GetDefaultScopeDescription(string scopeName)
    {
        return scopeName switch
        {
            Scopes.OpenId => "Your basic identity",
            Scopes.Profile => "Your basic profile details",
            Scopes.Email => "Your email address",
            Scopes.Phone => "Your phone number",
            Scopes.Address => "Your address details",
            "offline_access" => "Access to your data while you are offline",
            _ => $"Access permission for {scopeName}",
        };
    }

    private static bool IsDefaultRequiredScope(string scopeName)
    {
        return scopeName == Scopes.OpenId;
    }
}