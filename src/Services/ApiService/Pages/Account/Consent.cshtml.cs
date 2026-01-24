using IAMMod.Managers;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiService.Pages.Account;

public class ConsentModel(
    AuthorizationManager authorizationManager,
    ClientManager clientManager,
    ScopeManager scopeManager,
    ConsentManager consentManager,
    ILogger<ConsentModel> logger
) : PageModel
{
    private readonly AuthorizationManager _authorizationManager = authorizationManager;
    private readonly ClientManager _clientManager = clientManager;
    private readonly ScopeManager _scopeManager = scopeManager;
    private readonly ConsentManager _consentManager = consentManager;
    private readonly ILogger<ConsentModel> _logger = logger;

    [BindProperty(SupportsGet = true, Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    [BindProperty(Name = "client_id")]
    public string ClientId { get; set; } = string.Empty;

    [BindProperty(Name = "scope")]
    public string Scope { get; set; } = string.Empty;

    [BindProperty(Name = "state")]
    public string? State { get; set; }

    [BindProperty(Name = "nonce")]
    public string? Nonce { get; set; }

    [BindProperty(Name = "code_challenge")]
    public string? CodeChallenge { get; set; }

    [BindProperty(Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }

    [BindProperty(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [BindProperty(Name = "response_type")]
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
            // Get client by ClientId
            var client = await _clientManager.FindAsync<Client>(c => c.ClientId == ClientId);
            if (client == null)
            {
                _logger.LogError("Client not found in database: {ClientId}", ClientId);
                return RedirectToPage("/Account/ConsentDenied");
            }

            // Save consent if user chose to remember
            if (RememberConsent)
            {
                await _consentManager.GrantConsentAsync(userId, client.Id, Scope, isPermanent: true);
                _logger.LogInformation("Permanent consent granted for user {UserId} and client {ClientId}", userId, ClientId);
            }
            else
            {
                // Grant temporary consent (30 days)
                await _consentManager.GrantConsentAsync(userId, client.Id, Scope, isPermanent: false);
                _logger.LogInformation("Temporary consent granted for user {UserId} and client {ClientId}", userId, ClientId);
            }

            // Redirect back to the authorize endpoint to continue the flow
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
            _logger.LogError(ex, "Error processing consent");
            return Page();
        }
    }

    private static string GetDefaultScopeDescription(string scopeName)
    {
        return scopeName switch
        {
            "openid" => "您的基本身份标识",
            "profile" => "您的基本个人信息（姓名等）",
            "email" => "您的电子邮箱地址",
            "phone" => "您的电话号码",
            "address" => "您的地址信息",
            "offline_access" => "在您离线时访问您的数据",
            _ => $"访问 {scopeName} 资源的权限",
        };
    }

    private static bool IsDefaultRequiredScope(string scopeName)
    {
        return scopeName == "openid";
    }
}

public class ScopeViewModel
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; }
}
