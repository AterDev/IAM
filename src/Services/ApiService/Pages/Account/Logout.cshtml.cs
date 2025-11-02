using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiService.Pages.Account;

public class LogoutModel(ILogger<LogoutModel> logger) : PageModel
{
    private readonly ILogger<LogoutModel> _logger = logger;

    [BindProperty(SupportsGet = true)]
    public string? PostLogoutRedirectUri { get; set; }

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
                return Redirect(PostLogoutRedirectUri);
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
