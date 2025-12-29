using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiService.Pages.Account;

public class LogoutModel(ILogger<LogoutModel> logger) : PageModel
{
    private readonly ILogger<LogoutModel> _logger = logger;

    [BindProperty(SupportsGet = true, Name = "post_logout_redirect_uri")]
    public string? PostLogoutRedirectUri { get; set; }

    [BindProperty(SupportsGet = true, Name = "state")]
    public string? State { get; set; }

    [BindProperty(SupportsGet = true, Name = "id_token_hint")]
    public string? IdTokenHint { get; set; }

    public string? UserName { get; set; }

    public void OnGet()
    {
        UserName = HttpContext.Session.GetString("UserName");
    }

    public IActionResult OnPost()
    {
        try
        {
            // Clear session
            HttpContext.Session.Clear();

            // Clear authentication cookies (if using cookie authentication)
            // HttpContext.SignOutAsync();

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
