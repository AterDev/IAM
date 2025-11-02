using AccessMod.Managers;
using Entity.AccessMod;
using IdentityMod.Managers;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiService.Pages.Account;

public class ConsentModel(
    AuthorizationManager authorizationManager,
    ClientManager clientManager,
    ScopeManager scopeManager,
    ILogger<ConsentModel> logger
) : PageModel
{
    private readonly AuthorizationManager _authorizationManager = authorizationManager;
    private readonly ClientManager _clientManager = clientManager;
    private readonly ScopeManager _scopeManager = scopeManager;
    private readonly ILogger<ConsentModel> _logger = logger;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string ClientId { get; set; } = string.Empty;

    [BindProperty]
    public string Scope { get; set; } = string.Empty;

    [BindProperty]
    public string? State { get; set; }

    [BindProperty]
    public string? Nonce { get; set; }

    [BindProperty]
    public string? CodeChallenge { get; set; }

    [BindProperty]
    public string? CodeChallengeMethod { get; set; }

    [BindProperty]
    public string? RedirectUri { get; set; }

    [BindProperty]
    public string? ResponseType { get; set; }

    [BindProperty]
    public bool RememberConsent { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string? ClientDescription { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<ScopeViewModel> RequestedScopes { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        // Get user from session
        var userId = HttpContext.Session.GetString("UserId");
        UserName = HttpContext.Session.GetString("UserName") ?? "Unknown User";

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage(
                "/Account/Login",
                new { returnUrl = Request.Path + Request.QueryString }
            );
        }

        // Parse query parameters
        if (!string.IsNullOrEmpty(Request.QueryString.Value))
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(
                Request.QueryString.Value
            );

            ClientId = query.TryGetValue("client_id", out var clientId)
                ? clientId.ToString()
                : string.Empty;
            Scope = query.TryGetValue("scope", out var scope) ? scope.ToString() : string.Empty;
            State = query.TryGetValue("state", out var state) ? state.ToString() : null;
            Nonce = query.TryGetValue("nonce", out var nonce) ? nonce.ToString() : null;
            CodeChallenge = query.TryGetValue("code_challenge", out var challenge)
                ? challenge.ToString()
                : null;
            CodeChallengeMethod = query.TryGetValue("code_challenge_method", out var method)
                ? method.ToString()
                : null;
            RedirectUri = query.TryGetValue("redirect_uri", out var redirectUri)
                ? redirectUri.ToString()
                : null;
            ResponseType = query.TryGetValue("response_type", out var responseType)
                ? responseType.ToString()
                : null;
        }

        // Load client information
        try
        {
            var client = await _clientManager.FindAsync<Client>(c => c.ClientId == ClientId);
            if (client != null)
            {
                ClientName = client.DisplayName;
                ClientDescription = client.Description;
            }
            else
            {
                ClientName = ClientId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load client {ClientId}", ClientId);
            ClientName = ClientId;
        }

        // Load requested scopes
        var scopeNames = Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var scopeName in scopeNames)
        {
            try
            {
                var scopeInfo = await _scopeManager.FindAsync<ApiScope>(s => s.Name == scopeName);
                RequestedScopes.Add(
                    new ScopeViewModel
                    {
                        Name = scopeName,
                        DisplayName = scopeInfo?.DisplayName ?? scopeName,
                        Description =
                            scopeInfo?.Description ?? GetDefaultScopeDescription(scopeName),
                        Required = scopeInfo?.Required ?? IsDefaultRequiredScope(scopeName),
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load scope {Scope}", scopeName);
                RequestedScopes.Add(
                    new ScopeViewModel
                    {
                        Name = scopeName,
                        DisplayName = scopeName,
                        Description = GetDefaultScopeDescription(scopeName),
                        Required = IsDefaultRequiredScope(scopeName),
                    }
                );
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        var userId = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        if (action == "deny")
        {
            // User denied authorization
            if (!string.IsNullOrEmpty(RedirectUri))
            {
                var errorUrl =
                    $"{RedirectUri}?error=access_denied&error_description=User denied authorization";
                if (!string.IsNullOrEmpty(State))
                {
                    errorUrl += $"&state={State}";
                }
                return Redirect(errorUrl);
            }
            return RedirectToPage("/Account/ConsentDenied");
        }

        // User allowed authorization
        try
        {
            // TODO: Create authorization code and save consent
            // For now, redirect back to the authorize endpoint to continue the flow

            var authorizeUrl =
                $"/connect/authorize?client_id={ClientId}&scope={Scope}&response_type={ResponseType}&redirect_uri={RedirectUri}";

            if (!string.IsNullOrEmpty(State))
            {
                authorizeUrl += $"&state={State}";
            }

            if (!string.IsNullOrEmpty(Nonce))
            {
                authorizeUrl += $"&nonce={Nonce}";
            }

            if (!string.IsNullOrEmpty(CodeChallenge))
            {
                authorizeUrl += $"&code_challenge={CodeChallenge}";
            }

            if (!string.IsNullOrEmpty(CodeChallengeMethod))
            {
                authorizeUrl += $"&code_challenge_method={CodeChallengeMethod}";
            }

            // Add consent granted flag
            authorizeUrl += "&consent_granted=true";

            return Redirect(authorizeUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process consent for client {ClientId}", ClientId);
            return Page();
        }
    }

    private static string GetDefaultScopeDescription(string scopeName)
    {
        return scopeName.ToLower() switch
        {
            "openid" => "访问您的基本身份信息",
            "profile" => "访问您的个人资料（姓名、头像等）",
            "email" => "访问您的邮箱地址",
            "phone" => "访问您的手机号码",
            "address" => "访问您的地址信息",
            "offline_access" => "在您离线时访问您的信息",
            _ => $"访问 {scopeName} 资源",
        };
    }

    private static bool IsDefaultRequiredScope(string scopeName)
    {
        return scopeName.ToLower() == "openid";
    }
}

public class ScopeViewModel
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
}
