using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IdentityMod.Managers;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Pages.Account;

public class LoginModel(
    UserManager userManager,
    AuthorizationManager authorizationManager,
    ILogger<LoginModel> logger) : PageModel
{
    private readonly UserManager _userManager = userManager;
    private readonly AuthorizationManager _authorizationManager = authorizationManager;
    private readonly ILogger<LoginModel> _logger = logger;

    [BindProperty]
    [Required(ErrorMessage = "请输入用户名或邮箱")]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "请输入密码")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ClientName { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        // Extract client information from return URL if it's an OAuth request
        if (!string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.Contains("client_id="))
        {
            try
            {
                var query = new Uri(ReturnUrl, UriKind.RelativeOrAbsolute).Query;
                var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(query);
                
                if (queryParams.TryGetValue("client_id", out var clientId))
                {
                    // TODO: Load client details from database
                    ClientName = clientId.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse client info from return URL");
            }
        }

        await Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Attempt to authenticate user
            var user = await _userManager.ValidateCredentialsAsync(Username, Password);
            
            if (user == null)
            {
                ErrorMessage = "用户名或密码错误";
                return Page();
            }

            // Check if user is locked out
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                ErrorMessage = "账号已被锁定，请稍后再试";
                return Page();
            }

            // Create authentication session
            // TODO: Implement proper authentication cookie/session
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.UserName);

            // Redirect to return URL or default page
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Account/LoginSuccess");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for user {Username}", Username);
            ErrorMessage = "登录过程中发生错误，请稍后重试";
            return Page();
        }
    }
}
