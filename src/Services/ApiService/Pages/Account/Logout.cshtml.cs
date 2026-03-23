using IAMMod.Managers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using SysClaimTypes = System.Security.Claims.ClaimTypes;

namespace ApiService.Pages.Account;

public class LogoutModel(SessionManager sessionManager, ILogger<LogoutModel> logger) : PageModel
{
    private readonly SessionManager _sessionManager = sessionManager;
    private readonly ILogger<LogoutModel> _logger = logger;

    [BindProperty(SupportsGet = true, Name = OpenIdConnectParameterNames.PostLogoutRedirectUri)]
    public string? PostLogoutRedirectUri { get; set; }

    [BindProperty(SupportsGet = true, Name = OpenIdConnectParameterNames.State)]
    public string? State { get; set; }

    [BindProperty(SupportsGet = true, Name = OpenIdConnectParameterNames.IdTokenHint)]
    public string? IdTokenHint { get; set; }

    public string? UserName { get; set; }

    public void OnGet()
    {
        UserName = HttpContext.Session.GetString("UserName");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var sid = User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value ?? HttpContext.Session.GetString("SessionId");
            var userIdClaim = User.FindFirst(SysClaimTypes.NameIdentifier)?.Value;

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

            // Clear session
            HttpContext.Session.Clear();

            // Clear authentication cookies (if using cookie authentication)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("User logged out successfully");

            // Redirect to post logout URI or home
            if (!string.IsNullOrEmpty(PostLogoutRedirectUri) && Uri.IsWellFormedUriString(PostLogoutRedirectUri, UriKind.Absolute))
            {
                var redirectUri = PostLogoutRedirectUri;
                if (!string.IsNullOrEmpty(State))
                {
                    redirectUri += $"?state={Uri.EscapeDataString(State)}";
                }
                return Redirect(redirectUri);
            }

            return RedirectToPage("/Account/LogoutSuccess");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return Page();
        }
    }
}
