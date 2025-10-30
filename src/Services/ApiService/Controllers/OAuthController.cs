using IdentityMod.Managers;
using IdentityMod.Models.OAuthDtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace ApiService.Controllers;

/// <summary>
/// OAuth 2.0 / OpenID Connect endpoint controller
/// </summary>
/// <remarks>
/// Implements OAuth 2.0 and OpenID Connect (OIDC) specification endpoints:
/// - Authorization endpoint for initiating authorization code flow
/// - Token endpoint for exchanging authorization codes and refresh tokens
/// - Device authorization for device flow
/// - Token revocation for invalidating tokens
/// - Introspection for validating tokens
/// 
/// Supports multiple grant types:
/// - authorization_code: Standard authorization code flow with PKCE support
/// - refresh_token: Refresh token rotation
/// - client_credentials: Service-to-service authentication
/// - urn:ietf:params:oauth:grant-type:device_code: Device flow for limited-input devices
/// 
/// All endpoints follow OAuth 2.0 and OIDC specifications.
/// </remarks>
[ApiController]
[Route("connect")]
[AllowAnonymous]
[Produces("application/json")]
public class OAuthController(
    AuthorizationManager authorizationManager,
    TokenManager tokenManager,
    DeviceFlowManager deviceFlowManager,
    ILogger<OAuthController> logger
    ) : ControllerBase
{
    private readonly AuthorizationManager _authorizationManager = authorizationManager;
    private readonly TokenManager _tokenManager = tokenManager;
    private readonly DeviceFlowManager _deviceFlowManager = deviceFlowManager;
    private readonly ILogger<OAuthController> _logger = logger;

    /// <summary>
    /// Authorization endpoint (OAuth 2.0 / OIDC)
    /// </summary>
    /// <param name="request">Authorization request parameters including client_id, redirect_uri, scope, etc.</param>
    /// <returns>Authorization response or redirect to login/consent page</returns>
    /// <response code="302">Redirects to login page if user not authenticated, or to redirect_uri with authorization code</response>
    /// <response code="400">If the authorization request is invalid</response>
    /// <remarks>
    /// This is the standard OAuth 2.0 authorization endpoint that initiates the authorization code flow.
    /// 
    /// Required parameters:
    /// - response_type: Must be "code" for authorization code flow
    /// - client_id: The client identifier
    /// - redirect_uri: Where to redirect after authorization
    /// - scope: Requested scopes (space-separated)
    /// 
    /// Optional parameters:
    /// - state: Opaque value for CSRF protection
    /// - code_challenge: PKCE code challenge
    /// - code_challenge_method: PKCE method (S256 or plain)
    /// - nonce: Value to associate client session with ID token
    /// 
    /// Example:
    /// GET /connect/authorize?response_type=code&amp;client_id=my_client&amp;redirect_uri=https://example.com/callback&amp;scope=openid%20profile&amp;state=xyz&amp;code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&amp;code_challenge_method=S256
    /// </remarks>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            var userId = User.FindFirst(OAuthConstants.ClaimTypes.Subject)?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = OAuthConstants.ErrorCodes.InvalidUser, error_description = "User ID not found in claims" });
            }

            // TODO: Check if consent is required
            // For now, auto-consent for demonstration

            // Handle response type
            if (request.ResponseType == OAuthConstants.ResponseTypes.Code)
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
                    Error = OAuthConstants.ErrorCodes.UnsupportedResponseType,
                    ErrorDescription = "Only authorization code flow is currently supported",
                    State = request.State
                };

                return Redirect(BuildRedirectUri(request.RedirectUri, errorResponse, request.ResponseMode));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing authorization request");
            return StatusCode(500, new { error = OAuthConstants.ErrorCodes.ServerError, error_description = "An error occurred processing the request" });
        }
    }

    /// <summary>
    /// Token endpoint (OAuth 2.0 / OIDC)
    /// </summary>
    /// <param name="request">Token request parameters (form-encoded)</param>
    /// <returns>Token response with access_token, refresh_token, and token metadata</returns>
    /// <response code="200">Returns the token response with access and refresh tokens</response>
    /// <response code="400">If the token request is invalid or authentication fails</response>
    /// <remarks>
    /// Standard OAuth 2.0 token endpoint supporting multiple grant types.
    /// 
    /// **Authorization Code Grant** (with PKCE):
    /// 
    ///     POST /connect/token
    ///     Content-Type: application/x-www-form-urlencoded
    ///     
    ///     grant_type=authorization_code
    ///     &amp;code=AUTHORIZATION_CODE
    ///     &amp;redirect_uri=https://example.com/callback
    ///     &amp;client_id=my_client
    ///     &amp;client_secret=my_secret (confidential clients only)
    ///     &amp;code_verifier=CODE_VERIFIER (PKCE)
    /// 
    /// **Refresh Token Grant**:
    /// 
    ///     POST /connect/token
    ///     Content-Type: application/x-www-form-urlencoded
    ///     
    ///     grant_type=refresh_token
    ///     &amp;refresh_token=REFRESH_TOKEN
    ///     &amp;client_id=my_client
    ///     &amp;client_secret=my_secret (confidential clients only)
    ///     &amp;scope=openid profile (optional, to request reduced scope)
    /// 
    /// **Client Credentials Grant**:
    /// 
    ///     POST /connect/token
    ///     Content-Type: application/x-www-form-urlencoded
    ///     
    ///     grant_type=client_credentials
    ///     &amp;client_id=my_client
    ///     &amp;client_secret=my_secret
    ///     &amp;scope=api.read api.write
    /// 
    /// **Response**:
    /// 
    ///     {
    ///       "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///       "token_type": "Bearer",
    ///       "expires_in": 3600,
    ///       "refresh_token": "REFRESH_TOKEN",
    ///       "scope": "openid profile email"
    ///     }
    /// 
    /// </remarks>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status400BadRequest)]
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
                Error = OAuthConstants.ErrorCodes.ServerError,
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
                return BadRequest(new { error = OAuthConstants.ErrorCodes.InvalidClient, error_description = "Invalid client ID" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing device authorization request");
            return StatusCode(500, new { error = OAuthConstants.ErrorCodes.ServerError, error_description = "An error occurred processing the request" });
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
            return StatusCode(500, new { error = OAuthConstants.ErrorCodes.ServerError, error_description = "An error occurred processing the request" });
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
            return StatusCode(500, new { error = OAuthConstants.ErrorCodes.ServerError, error_description = "An error occurred processing the request" });
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
            return StatusCode(500, new { error = OAuthConstants.ErrorCodes.ServerError, error_description = "An error occurred processing the request" });
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
