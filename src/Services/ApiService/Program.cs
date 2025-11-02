using ApiService.Extension;
using Ater.Web.Convention;
using CommonMod;
using IdentityMod;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 共享基础服务:health check, service discovery, opentelemetry, http retry etc.
builder.AddServiceDefaults();

// 框架依赖服务:options, cache, dbContext
builder.AddFrameworkServices();

// 添加CommonMod服务
builder.AddCommonMod();

// 添加IdentityMod服务
builder.AddIdentityModMod();

// Web中间件服务:route, openapi, jwt, cors, auth, rateLimiter etc.
builder.AddMiddlewareServices();

// Add session support for OAuth pages
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add Razor Pages for OAuth UI (login, consent, logout)
builder.Services.AddRazorPages();

builder
    .Services.AddAuthorizationBuilder()
    .AddPolicy(
        WebConst.User,
        policy =>
        {
            policy.RequireRole(WebConst.User);
        }
    );

// Managers, auto generate by source generator
builder.Services.AddManagers();

// Modules, auto generate by source generator
builder.AddModules();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

// Enable session middleware
app.UseSession();

// 使用中间件
app.UseMiddlewareServices();

// Map Razor Pages
app.MapRazorPages();

await app.RunAsync();
