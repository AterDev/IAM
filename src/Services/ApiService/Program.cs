using AccessMod;
using ApiService.Extension;
using CommonMod;
using IdentityMod;
using Perigon.AspNetCore.Constants;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 共享基础服务:health check, service discovery, opentelemetry, http retry etc.
builder.AddServiceDefaults();

// 框架依赖服务:options, cache, dbContext
builder.AddFrameworkServices();

// 添加 CommonMod 服务
builder.AddCommonMod();

// 添加 AccessMod 服务（包括密钥管理）
builder.AddAccessMod();

// 添加 IdentityMod 服务
builder.AddIdentityMod();

// Web 中间件服务:route, openapi, jwt, cors, auth, rateLimiter etc.
builder.AddMiddlewareServices();

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


