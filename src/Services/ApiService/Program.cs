using AccessMod;
using ApiService.Extension;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Perigon.AspNetCore.Constants;
using Perigon.AspNetCore.Options;
using ServiceDefaults.Middleware;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 共享基础服务:health check, service discovery, opentelemetry, http retry etc.
builder.AddServiceDefaults();

// 框架依赖服务:options, cache, dbContext
builder.AddFrameworkServices();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthentication(options =>
{
    // 对于 API 默认使用 JWT
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    // 但对于 Razor Pages 使用 Cookie
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddJwtBearer(cfg =>
{
    cfg.SaveToken = true;
    var jwtOption = builder.Configuration.GetSection(JwtOption.ConfigPath).Get<JwtOption>();
    var sign = jwtOption?.Sign;
    if (string.IsNullOrEmpty(sign))
    {
        throw new Exception("未找到有效的Jwt配置");
    }
    cfg.TokenValidationParameters = new TokenValidationParameters()
    {

        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sign)),
        ValidIssuer = jwtOption?.ValidIssuer,
        ValidAudience = jwtOption?.ValidAudiences,
        ValidateIssuer = true,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ValidateIssuerSigningKey = true,
    };
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(WebConst.User, policy =>
        {
            policy.RequireRole(WebConst.User);
        }
    );
builder.Services.AddLocalizer();
// Add Razor Pages for OAuth UI (login, consent, logout)
builder.Services.AddRazorPages();

builder.Services.AddManagers();
builder.AddModules();


WebApplication app = builder.Build();

app.UseSession();
app.UseRouting();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();
app.MapControllers();
app.MapRazorPages();
app.MapFallbackToFile("index.html");


using (app)
{
    // 在启动前执行初始化操作
    await using (var scope = app.Services.CreateAsyncScope())
    {
        IServiceProvider provider = scope.ServiceProvider;
        await InitModule.InitializeAsync(provider);
    }
    app.Run();
}


