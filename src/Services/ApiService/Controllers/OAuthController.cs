using IdentityMod.Managers;
using IdentityMod.Models.OAuthDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiService.Controllers;

/// <summary>
/// OAuth/OIDC Connect endpoints controller
/// </summary>
[ApiController]
[Route("connect")]
[AllowAnonymous]
public class OAuthController : ControllerBase
{
    private readonly AuthorizationManager _authorizationManager;
    private readonly TokenManager _tokenManager;
    private readonly DeviceFlowManager _deviceFlowManager;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        AuthorizationManager authorizationManager,
        TokenManager tokenManager,
        DeviceFlowManager deviceFlowManager,
        ILogger<OAuthController> logger
    )
    {
        _authorizationManager = authorizationManager;
        _tokenManager = tokenManager;
        _deviceFlowManager = deviceFlowManager;
        _logger = logger;
    }

    /// <summary>
    /// Authorization endpoint (OAuth 2.0 / OIDC)
    /// </summary>
    /// <param name="request">Authorization request parameters</param>
    /// <returns>Authorization response or redirect</returns>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    public async Task<IActionResult> Authorize([FromQuery] AuthorizeRequestDto request)
    {
        try
        {
            // Validate authorization request
            var (isValid, error, client) = await _authorizationManager.ValidateAuthorizationRequestAsync(
                request
            );

            if (!isValid)
            {
                // Return error response
                var errorResponse = new AuthorizeResponseDto
                {
                    Error = error,
                    ErrorDescription = $"Authorization request validation failed: {error}",
                    State = request.State
                };

                // Redirect to callback with error if redirect URI is available
                if (client != null && !string.IsNullOrEmpty(request.RedirectUri))
                {
                    return Redirect(BuildRedirectUri(request.RedirectUri, errorResponse, request.ResponseMode));
                }

                return BadRequest(errorResponse);
            }

            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                // Redirect to login page with return URL
                var returnUrl = Request.Path + Request.QueryString;
                return Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            // Get user ID from claims
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "invalid_user", error_description = "User ID not found in claims" });
            }

            // TODO: Check if consent is required
            // For now, auto-consent for demonstration

            // Handle response type
            if (request.ResponseType == "code")
            {
                // Authorization code flow
                var code = await _authorizationManager.CreateAuthorizationCodeAsync(
                    userId,
                    client!.Id,
                    request.RedirectUri,
                    request.Scope,
                    request.CodeChallenge,
                    request.CodeChallengeMethod,
                    request.Nonce
                );

                var response = new AuthorizeResponseDto
                {
                    Code = code,
                    State = request.State
                };

                return Redirect(BuildRedirectUri(request.RedirectUri, response, request.ResponseMode));
            }
            else
            {
                // Unsupported response type (implicit/hybrid flows not implemented yet)
                var errorResponse = new AuthorizeResponseDto
                {
                    Error = "unsupported_response_type",
                    ErrorDescription = "Only authorization code flow is currently supported",
                    State = request.State
                };

                return Redirect(BuildRedirectUri(request.RedirectUri, errorResponse, request.ResponseMode));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing authorization request");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred processing the request" });
        }
    }

    /// <summary>
    /// Token endpoint (OAuth 2.0 / OIDC)
    /// </summary>
    /// <param name="request">Token request parameters</param>
    /// <returns>Token response</returns>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm] TokenRequestDto request)
    {
        try
        {
            var response = await _tokenManager.ProcessTokenRequestAsync(request);

            if (!string.IsNullOrEmpty(response.Error))
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing token request");
            return StatusCode(500, new TokenResponseDto
            {
                Error = "server_error",
                ErrorDescription = "An error occurred processing the request"
            });
        }
    }

    /// <summary>
    /// Device authorization endpoint (RFC 8628)
    /// </summary>
    /// <param name="request">Device authorization request</param>
    /// <returns>Device authorization response</returns>
    [HttpPost("device")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> DeviceAuthorization([FromForm] DeviceAuthorizationRequestDto request)
    {
        try
        {
            var response = await _deviceFlowManager.InitiateDeviceAuthorizationAsync(request);

            if (response == null)
            {
                return BadRequest(new { error = "invalid_client", error_description = "Invalid client ID" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing device authorization request");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred processing the request" });
        }
    }

    /// <summary>
    /// Token introspection endpoint (RFC 7662)
    /// </summary>
    /// <param name="request">Introspection request</param>
    /// <returns>Introspection response</returns>
    [HttpPost("introspect")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Introspect([FromForm] IntrospectRequestDto request)
    {
        try
        {
            var response = await _tokenManager.IntrospectTokenAsync(request.Token, request.TokenTypeHint);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error introspecting token");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred processing the request" });
        }
    }

    /// <summary>
    /// Token revocation endpoint (RFC 7009)
    /// </summary>
    /// <param name="request">Revocation request</param>
    /// <returns>Success response</returns>
    [HttpPost("revoke")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Revoke([FromForm] RevokeRequestDto request)
    {
        try
        {
            await _tokenManager.RevokeTokenAsync(request.Token, request.TokenTypeHint);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred processing the request" });
        }
    }

    /// <summary>
    /// Logout endpoint (OIDC)
    /// </summary>
    /// <param name="request">Logout request</param>
    /// <returns>Redirect response</returns>
    [HttpGet("logout")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromQuery] LogoutRequestDto request)
    {
        try
        {
            // Sign out the user
            await HttpContext.SignOutAsync();

            // Redirect to post logout URI if provided
            if (!string.IsNullOrEmpty(request.PostLogoutRedirectUri))
            {
                var redirectUri = request.PostLogoutRedirectUri;
                if (!string.IsNullOrEmpty(request.State))
                {
                    redirectUri += $"?state={Uri.EscapeDataString(request.State)}";
                }
                return Redirect(redirectUri);
            }

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing logout request");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred processing the request" });
        }
    }

    /// <summary>
    /// Build redirect URI with query parameters
    /// </summary>
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
}
