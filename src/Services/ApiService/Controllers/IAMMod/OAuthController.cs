using IAMMod.Managers;
using IAMMod.Models.OAuthDtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using Share.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SysClaimTypes = System.Security.Claims.ClaimTypes;

namespace ApiService.Controllers.IAMMod;

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
/// - UserInfo endpoint for retrieving authenticated user claims
/// 
/// Supports multiple grant types:
/// - authorization_code: Standard authorization code flow with PKCE support
/// - refresh_token: Refresh token rotation
/// - client_credentials: Service-to-service authentication
/// - urn:ietf:params:oauth:grant-type:device_code: Device flow for limited-input devices
/// 
/// All endpoints follow OAuth 2.0 and OIDC specifications.
/// </remarks>
[Route("connect")]
public class OAuthController(
    AuthorizationManager authorizationManager,
    TokenManager tokenManager,
    DeviceFlowManager deviceFlowManager,
    ConsentManager consentManager,
    ClientManager clientManager,
    ScopeManager scopeManager,
    DiscoveryManager discoveryManager,
    SigningKeyManager signingKeyManager,
    SessionManager sessionManager,
    Localizer localizer,
    ILogger<OAuthController> logger
    ) : RestControllerBase(localizer)
{
    private readonly AuthorizationManager _authorizationManager = authorizationManager;
    private readonly TokenManager _tokenManager = tokenManager;
    private readonly DeviceFlowManager _deviceFlowManager = deviceFlowManager;
    private readonly ConsentManager _consentManager = consentManager;
    private readonly ClientManager _clientManager = clientManager;
    private readonly ScopeManager _scopeManager = scopeManager;
    private readonly DiscoveryManager _discoveryManager = discoveryManager;
    private readonly SigningKeyManager _signingKeyManager = signingKeyManager;
    private readonly SessionManager _sessionManager = sessionManager;
    private readonly ILogger<OAuthController> _logger = logger;

    /// <summary>
    /// Authorization endpoint (OAuth 2.0 / OIDC)
    /// </summary>
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
    public async Task<ActionResult> Authorize([FromQuery] AuthorizeRequestDto request)
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

            // Authenticate using Cookie scheme for web-based OAuth flow
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Check if user is authenticated via Cookie
            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                // Redirect to login page with return URL
                var returnUrl = Request.Path + Request.QueryString;
                return Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            // Get user ID from cookie claims
            var userId = authenticateResult.Principal.FindFirst(OAuthConst.JwtClaimNames.Subject)?.Value
                ?? authenticateResult.Principal.FindFirst(SysClaimTypes.NameIdentifier)?.Value
                ?? HttpContext.Session.GetString("UserId");
            var sessionId = authenticateResult.Principal.FindFirst("sid")?.Value
                ?? HttpContext.Session.GetString("SessionId");

            if (string.IsNullOrEmpty(userId))
            {
                // Redirect to login
                var returnUrl = Request.Path + Request.QueryString;
                return Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            // Check if user has already granted consent for this client and scopes
            var hasValidConsent = await _consentManager.HasValidConsentAsync(userId, client!.Id, request.Scope ?? string.Empty);

            // Check if consent is required and not yet granted
            var consentGranted = Request.Query.ContainsKey("consent_granted") && Request.Query["consent_granted"] == "true";

            if (!hasValidConsent && !consentGranted)
            {
                // Redirect to consent page
                var consentUrl = $"/Account/Consent{Request.QueryString}";
                return Redirect(consentUrl);
            }

            // Handle response type
            if (request.ResponseType == ResponseTypes.Code)
            {
                // Authorization code flow
                var code = await _authorizationManager.CreateAuthorizationCodeAsync(
                    userId,
                    client!.Id,
                    request.RedirectUri,
                    request.Scope,
                    request.CodeChallenge,
                    request.CodeChallengeMethod,
                    request.Nonce,
                    sessionId
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
                    Error = ErrorCodes.UnsupportedResponseType,
                    ErrorDescription = "Only authorization code flow is currently supported",
                    State = request.State
                };

                return Redirect(BuildRedirectUri(request.RedirectUri, errorResponse, request.ResponseMode));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing authorization request");
            return StatusCode(500, new { error = ErrorCodes.ServerError, error_description = "An error occurred processing the request" });
        }
    }

    /// <summary>
    /// Token endpoint (OAuth 2.0 / OIDC)
    /// </summary>
    /// <param name="request">Token request parameters</param>
    /// <returns>Token response with access token, refresh token, etc.</returns>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [EnableRateLimiting(WebConst.TokenEndpoint)]
    public async Task<ActionResult<TokenResponseDto>> Token([FromForm] TokenRequestDto request)
    {
        try
        {
            var signingKey = await _signingKeyManager.GetActiveSigningKeyAsync();
            if (signingKey == null)
            {
                return Problem(Localizer.BadRequest);
            }

            var response = await _tokenManager.ProcessTokenRequestAsync(request, signingKey);

            // Assuming response will always be valid, no error handling needed here

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing token request");
            return Problem();
        }
    }

    /// <summary>
    /// Device authorization endpoint (RFC 8628)
    /// </summary>
    /// <param name="request">Device authorization request</param>
    /// <returns>Device authorization response</returns>
    [HttpPost("device")]
    [Consumes("application/x-www-form-urlencoded")]
    [EnableRateLimiting(WebConst.DeviceEndpoint)]
    public async Task<ActionResult<DeviceAuthorizationResponseDto>> DeviceAuthorization([FromForm] DeviceAuthorizationRequestDto request)
    {
        try
        {
            var response = await _deviceFlowManager.InitiateDeviceAuthorizationAsync(request);

            if (response == null)
            {
                return BadRequest(Localizer.BadRequest);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing device authorization request");
            return Problem();
        }
    }

    /// <summary>
    /// Token introspection endpoint (RFC 7662)
    /// </summary>
    /// <param name="request">Introspection request</param>
    /// <returns>Introspection response</returns>
    [HttpPost("introspect")]
    [Consumes("application/x-www-form-urlencoded")]
    [EnableRateLimiting(WebConst.TokenEndpoint)]
    public async Task<ActionResult<IntrospectResponseDto>> Introspect([FromForm] IntrospectRequestDto request)
    {
        try
        {
            var (clientId, clientSecret) = ResolveClientCredentials(request.ClientId, request.ClientSecret);
            var client = await _tokenManager.ValidateSensitiveEndpointClientAsync(clientId, clientSecret);
            if (client == null)
            {
                return Unauthorized(new { error = ErrorCodes.InvalidClient, error_description = "Client authentication failed." });
            }

            var response = await _tokenManager.IntrospectTokenAsync(request.Token, request.TokenTypeHint, client.Id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error introspecting token");
            return Problem();
        }
    }

    /// <summary>
    /// Token revocation endpoint (RFC 7009)
    /// </summary>
    /// <remarks>
    /// Revocation endpoint returns 200 OK on success (RFC 7009 allows omitting response body).
    /// Errors are returned as JSON with error code and description.
    /// </remarks>
    /// <param name="request">Revocation request</param>
    /// <returns>Success response or error</returns>
    [HttpPost("revoke")]
    [Consumes("application/x-www-form-urlencoded")]
    [EnableRateLimiting(WebConst.TokenEndpoint)]
    public async Task<ActionResult> Revoke([FromForm] RevokeRequestDto request)
    {
        try
        {
            var (clientId, clientSecret) = ResolveClientCredentials(request.ClientId, request.ClientSecret);
            var client = await _tokenManager.ValidateSensitiveEndpointClientAsync(clientId, clientSecret);
            if (client == null)
            {
                return Unauthorized(new { error = ErrorCodes.InvalidClient, error_description = "Client authentication failed." });
            }

            await _tokenManager.RevokeTokenAsync(request.Token, request.TokenTypeHint, client.Id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return Problem();
        }
    }

    /// <summary>
    /// Logout endpoint (OIDC)
    /// </summary>
    /// <param name="request">Logout request</param>
    /// <returns>Redirect response to post_logout_redirect_uri or success message</returns>
    [HttpGet("logout")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Logout([FromQuery] LogoutRequestDto request)
    {
        try
        {
            var sid = User.FindFirst("sid")?.Value ?? HttpContext.Session.GetString("SessionId");
            var userIdClaim = User.FindFirst(SysClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(OAuthConst.JwtClaimNames.Subject)?.Value;

            if (!string.IsNullOrWhiteSpace(sid) && Guid.TryParse(userIdClaim, out var userId))
            {
                var session = await _sessionManager.GetBySessionIdAsync(sid);
                if (session != null)
                {
                    await _sessionManager.RevokeSessionAsync(
                        session.Id,
                        userId.ToString(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        HttpContext.Request.Headers.UserAgent.ToString()
                    );
                }
            }

            HttpContext.Session.Clear();
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
            return StatusCode(500, new { error = ErrorCodes.ServerError, error_description = "An error occurred processing the request" });
        }
    }

    /// <summary>
    /// UserInfo endpoint (OIDC)
    /// </summary>
    /// <returns>Claims about the authenticated user</returns>
    /// <response code="200">Returns user information claims</response>
    /// <response code="401">If the access token is invalid or missing</response>
    /// <response code="403">If the token does not have sufficient scope</response>
    /// <remarks>
    /// Returns claims about the authenticated End-User as defined in OpenID Connect Core 1.0.
    /// This endpoint requires a valid access token obtained through the OAuth 2.0 flow.
    ///
    /// The returned claims depend on:
    /// - The scopes granted in the access token (profile, email, phone, address)
    /// - The user's actual profile data
    /// - The client's allowed scopes
    ///
    /// Standard scopes and their claims:
    /// - profile: name, family_name, given_name, middle_name, nickname, preferred_username,
    ///   profile, picture, website, gender, birthdate, zoneinfo, locale, updated_at
    /// - email: email, email_verified
    /// - phone: phone_number, phone_number_verified
    /// - address: address (structured claim)
    ///
    /// Request must include Authorization header:
    /// Authorization: Bearer {access_token}
    ///
    /// Example:
    /// GET /connect/userinfo
    /// Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
    ///
    /// Response:
    /// {
    ///   "sub": "248289761001",
    ///   "name": "Jane Doe",
    ///   "email": "jane.doe@example.com",
    ///   "email_verified": true
    /// }
    /// </remarks>
    [HttpGet("userinfo")]
    [Authorize]
    public async Task<ActionResult<UserInfoDto>> UserInfo()
    {
        try
        {
            // Get user ID from the token claims
            var userIdClaim =
                User.FindFirst(SysClaimTypes.NameIdentifier)
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("UserInfo request with invalid or missing subject claim");
                return Unauthorized(
                    new
                    {
                        error = "invalid_token",
                        error_description = "The access token is invalid or does not contain a valid subject",
                    }
                );
            }

            // Parse scopes from token
            var scopes = ParseScopesFromToken(User);

            // Get user information
            var userInfo = await _discoveryManager.GetUserInfoAsync(userId, scopes);

            if (userInfo == null)
            {
                _logger.LogWarning("User {UserId} not found for UserInfo request", userId);
                return NotFound(
                    new
                    {
                        error = "user_not_found",
                        error_description = "The user associated with this token was not found",
                    }
                );
            }

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user info");
            return Problem("Failed to retrieve user information", statusCode: 500);
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

    /// <summary>
    /// Parse scopes from the token claims
    /// </summary>
    /// <param name="principal">The claims principal from the token</param>
    /// <returns>List of scope strings</returns>
    private static List<string> ParseScopesFromToken(ClaimsPrincipal principal)
    {
        var scopes = new List<string>();

        var scopeValue = principal.FindFirst("scope")?.Value;

        if (!string.IsNullOrWhiteSpace(scopeValue))
        {
            // Split space-separated scopes
            scopes.AddRange(scopeValue.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        return scopes;
    }

    private (string? clientId, string? clientSecret) ResolveClientCredentials(string? clientId, string? clientSecret)
    {
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            return (clientId, clientSecret);
        }

        if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorizationHeader))
        {
            return (null, null);
        }

        var value = authorizationHeader.ToString();
        if (!value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(value[6..].Trim()));
            var separatorIndex = raw.IndexOf(':');
            if (separatorIndex < 0)
            {
                return (null, null);
            }

            return (raw[..separatorIndex], raw[(separatorIndex + 1)..]);
        }
        catch (FormatException)
        {
            return (null, null);
        }
    }

    private async Task<List<OAuthInteractionScopeDto>> BuildScopeDtosAsync(string? scope)
    {
        var scopeNames = (scope ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var results = new List<OAuthInteractionScopeDto>();
        foreach (var scopeName in scopeNames)
        {
            var scopeInfo = await _scopeManager.FindAsync<ApiScope>(s => s.Name == scopeName);
            results.Add(new OAuthInteractionScopeDto
            {
                Name = scopeName,
                DisplayName = scopeInfo?.DisplayName ?? scopeName,
                Description = scopeInfo?.Description ?? GetDefaultScopeDescription(scopeName),
                Required = scopeInfo?.Required ?? IsDefaultRequiredScope(scopeName),
            });
        }

        return results;
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
