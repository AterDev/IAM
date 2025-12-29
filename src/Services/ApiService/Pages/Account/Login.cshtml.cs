using System.ComponentModel.DataAnnotations;
using IdentityMod.Managers;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
    [Display(Name = "用户名或邮箱")]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "请输入密码")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "记住我")]
    public bool RememberMe { get; set; }

    [BindProperty(SupportsGet = true, Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    public string? ClientName { get; set; }

    public async Task OnGetAsync()
    {
        // Extract client information from return URL if it's an OAuth request
        if (!string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.Contains("client_id="))
        {
            try
            {
                var queryStartIndex = ReturnUrl.IndexOf('?');
                if (queryStartIndex >= 0)
                {
                    var queryString = ReturnUrl.Substring(queryStartIndex);
                    var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString);

                    if (queryParams.TryGetValue("client_id", out var clientId))
                    {
                        ClientName = clientId.ToString();
                    }
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
                _logger.LogWarning("Authentication failed for user: {Username}", Username);
                ModelState.AddModelError(string.Empty, "用户名或密码错误");
                return Page();
            }

            // Check if user is locked out
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("User {Username} is locked out until {LockoutEnd}", Username, user.LockoutEnd);
                ModelState.AddModelError(string.Empty, "账号已被锁定，请稍后再试");
                return Page();
            }

            _logger.LogInformation("User {Username} logged in successfully", Username);

            // Store user info in session for OAuth flow
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
            ModelState.AddModelError(string.Empty, "登录过程中发生错误，请稍后重试");
            return Page();
        }
    }
}
