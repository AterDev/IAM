using System.Net;
using System.Text;

namespace ApiService.Browser;

internal static class BrowserHtml
{
    private const string Style = """
        :root { color-scheme: light; font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
        * { box-sizing: border-box; }
        body { margin: 0; min-height: 100vh; display: grid; place-items: center; padding: 24px; background: linear-gradient(135deg, #eef2ff, #e0f2fe); color: #172033; }
        main { width: min(100%, 480px); padding: 32px; border-radius: 18px; background: #fff; box-shadow: 0 18px 60px #17203324; }
        h1 { margin: 0 0 8px; font-size: 28px; } h2 { margin: 24px 0 12px; font-size: 18px; }
        p { color: #526075; line-height: 1.6; } label { display: block; margin: 18px 0 7px; font-weight: 600; }
        input[type=text], input[type=email], input[type=password] { width: 100%; padding: 12px 14px; border: 1px solid #c7d0df; border-radius: 10px; font-size: 16px; }
        input[type=checkbox] { width: 16px; height: 16px; vertical-align: middle; } .check { margin: 18px 0; font-weight: 400; }
        button, .button { display: inline-block; width: 100%; padding: 12px 16px; border: 0; border-radius: 10px; background: #315bdc; color: #fff; font-size: 16px; font-weight: 650; text-align: center; text-decoration: none; cursor: pointer; }
        button.secondary, .button.secondary { background: #64748b; } button.danger { background: #c63b4a; }
        .actions { display: flex; gap: 12px; margin-top: 24px; } .actions > * { flex: 1; }
        .error { padding: 12px 14px; border-radius: 10px; background: #fff1f2; color: #a61b2b; border: 1px solid #fecdd3; }
        .info { padding: 14px; border-radius: 10px; background: #eff6ff; color: #1e40af; } .scope { display: flex; gap: 12px; padding: 12px 0; border-bottom: 1px solid #e2e8f0; }
        .scope:last-child { border-bottom: 0; } .scope strong { display: block; } .muted { color: #64748b; font-size: 14px; }
        a { color: #315bdc; }
        """;

    internal static string Login(string token, string? returnUrl, string? clientName, string email, bool rememberMe, IReadOnlyList<string> errors)
    {
        var errorHtml = Errors(errors);
        var clientHtml = string.IsNullOrWhiteSpace(clientName)
            ? string.Empty
            : $"<p class=\"info\"><strong>{Encode(clientName)}</strong> 请求访问您的账号。</p>";
        var returnField = Hidden("returnUrl", returnUrl);
        var checkedAttribute = rememberMe ? " checked" : string.Empty;
        return Layout("登录", $"""
            <main>
              <h1>🔐 IAM</h1><p>身份与访问管理系统</p>
              {clientHtml}{errorHtml}
              <form method="post" action="/Account/Login">
                {Hidden("__RequestVerificationToken", token)}{returnField}
                <label for="email">邮箱</label><input id="email" name="email" type="email" value="{Encode(email)}" placeholder="请输入邮箱" autocomplete="email" autofocus required />
                <label for="password">密码</label><input id="password" name="password" type="password" placeholder="请输入密码" autocomplete="current-password" required />
                <label class="check"><input name="rememberMe" type="checkbox" value="true"{checkedAttribute} /> 记住我</label>
                <button type="submit">登录</button>
              </form>
            </main>
            """);
    }

    internal static string Mfa(string token, PendingMfaState state, string code, IReadOnlyList<string> errors)
    {
        return Layout("多因素验证", $"""
            <main>
              <p class="info">当前账号：<strong>{Encode(state.UserName)}</strong></p>
              <h1>多因素验证</h1><p>请输入身份验证器中的 6 位验证码，或输入一枚恢复码以完成登录。</p>
              {Errors(errors)}
              <form method="post" action="/Account/Mfa">
                {Hidden("__RequestVerificationToken", token)}
                <label for="code">验证码或恢复码</label><input id="code" name="code" type="text" value="{Encode(code)}" autocomplete="one-time-code" autofocus required />
                <button type="submit">验证并继续</button>
              </form>
              <p class="muted">恢复码只能使用一次。</p>
            </main>
            """);
    }

    internal static string Consent(string token, string clientName, string? clientDescription, string userName, IReadOnlyList<ScopeViewModel> scopes, ConsentForm form, IReadOnlyList<string> errors)
    {
        var scopeHtml = new StringBuilder();
        foreach (var scope in scopes)
        {
            var required = scope.Required ? " <span class=\"muted\">必需</span>" : string.Empty;
            scopeHtml.Append($"<div class=\"scope\"><span>✓</span><div><strong>{Encode(scope.DisplayName)}{required}</strong><span class=\"muted\">{Encode(scope.Description)}</span></div></div>");
        }

        var remember = form.RememberConsent ? " checked" : string.Empty;
        return Layout("授权确认", $"""
            <main>
              <h1>授权确认</h1><p>请审查并确认授权。</p>
              <p class="info"><strong>{Encode(clientName)}</strong><br />{Encode(clientDescription)}</p>
              <p>您将以 <strong>{Encode(userName)}</strong> 的身份授权此应用。</p>
              <h2>该应用请求以下权限</h2>{scopeHtml}{Errors(errors)}
              <form method="post" action="/Account/Consent">
                {Hidden("__RequestVerificationToken", token)}{form.HiddenFields()}
                <label class="check"><input name="rememberConsent" type="checkbox" value="true"{remember} /> 记住我的选择，不再询问</label>
                <div class="actions"><button type="submit" name="action" value="allow">允许访问</button><button class="secondary" type="submit" name="action" value="deny">拒绝</button></div>
              </form>
            </main>
            """);
    }

    internal static string Logout(string token, string? userName, LogoutForm form)
    {
        var user = string.IsNullOrWhiteSpace(userName) ? string.Empty : $"<p class=\"info\">当前登录用户：<strong>{Encode(userName)}</strong></p>";
        return Layout("确认登出", $"""
            <main>
              <h1>确认登出</h1><p>您确定要登出吗？</p>{user}
              <form method="post" action="/Account/Logout">
                {Hidden("__RequestVerificationToken", token)}{form.HiddenFields()}
                <button class="danger" type="submit">确认登出</button>
              </form>
              <p><a href="{Encode(form.PostLogoutRedirectUri ?? "/")}">取消</a></p>
            </main>
            """);
    }

    internal static string Success(string message) => Layout("操作完成", $"<main><h1>操作完成</h1><p>{Encode(message)}</p><a class=\"button\" href=\"/\">返回首页</a></main>");

    internal static string Error(string message) => Layout("请求无法完成", $"<main><h1>请求无法完成</h1><p class=\"error\">{Encode(message)}</p><a href=\"/\">返回首页</a></main>");

    private static string Layout(string title, string content) => $"<!doctype html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{Encode(title)} - IAM</title><style>{Style}</style></head><body>{content}</body></html>";

    private static string Errors(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            return string.Empty;
        }

        var items = string.Join(string.Empty, errors.Select(error => $"<li>{Encode(error)}</li>"));
        return $"<div class=\"error\"><ul>{items}</ul></div>";
    }

    private static string Hidden(string name, string? value) => $"<input type=\"hidden\" name=\"{Encode(name)}\" value=\"{Encode(value)}\">";

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}

internal sealed record ScopeViewModel(string Name, string DisplayName, string? Description, bool Required);

internal sealed record ConsentForm(
    string ClientId,
    string Scope,
    string? State,
    string? Nonce,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string? RedirectUri,
    string? ResponseType,
    bool RememberConsent)
{
    internal string HiddenFields() => string.Concat(
        Hidden("client_id", ClientId),
        Hidden("scope", Scope),
        Hidden("state", State),
        Hidden("nonce", Nonce),
        Hidden("code_challenge", CodeChallenge),
        Hidden("code_challenge_method", CodeChallengeMethod),
        Hidden("redirect_uri", RedirectUri),
        Hidden("response_type", ResponseType));

    private static string Hidden(string name, string? value) => $"<input type=\"hidden\" name=\"{WebUtility.HtmlEncode(name)}\" value=\"{WebUtility.HtmlEncode(value ?? string.Empty)}\">";
}

internal sealed record LogoutForm(string? PostLogoutRedirectUri, string? State, string? IdTokenHint)
{
    internal string HiddenFields() => string.Concat(
        Hidden("post_logout_redirect_uri", PostLogoutRedirectUri),
        Hidden("state", State),
        Hidden("id_token_hint", IdTokenHint));

    private static string Hidden(string name, string? value) => $"<input type=\"hidden\" name=\"{WebUtility.HtmlEncode(name)}\" value=\"{WebUtility.HtmlEncode(value ?? string.Empty)}\">";
}
