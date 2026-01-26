using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceDefaults.Middleware;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace IAMMod;

public static class ModuleExtensions
{
    /// <summary>
    /// 注册 IAMMod 模块服务，包括密钥管理与 OAuth 核心逻辑
    /// </summary>
    [DisplayName("Perigon::IAMMod")]
    [Description("注册 IAMMod 模块服务，包括密钥管理与 OAuth 核心逻辑")]
    public static IHostApplicationBuilder AddIAMMod(this IHostApplicationBuilder builder)
    {
        // 注册 Share 层的 OAuthService
        builder.Services.AddScoped<OAuthService>();
        builder.AddModServices();
        return builder;
    }

    private static IHostApplicationBuilder AddModServices(this IHostApplicationBuilder builder)
    {

        builder.Services.AddSwagger();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(AppConst.Default, policy =>
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
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme)
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
            .AddPolicy(WebConst.AdminUser, policy =>
            {
                policy.RequireRole(WebConst.AdminUser, WebConst.SuperAdmin);
            }
            );
        builder.Services.AddLocalizer();
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
                options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
            });
        // Add Razor Pages for OAuth UI (login, consent, logout)
        builder.Services.AddRazorPages();
        builder.Services.AddSingleton<SigningKeyResolver>();
        builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>>(sp =>
            new ConfigureNamedOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var jwtOption = sp.GetRequiredService<IOptions<JwtOption>>().Value;
                if (string.IsNullOrWhiteSpace(jwtOption.ValidIssuer) || string.IsNullOrWhiteSpace(jwtOption.ValidAudiences))
                {
                    throw new InvalidOperationException("未找到有效的Jwt配置");
                }

                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKeyResolver = (token, securityToken, keyId, parameters) =>
                    {
                        // Auth center validates tokens it issued: use local DB-cached signing keys.
                        var resolver = sp.GetRequiredService<SigningKeyResolver>();
                        return resolver.Resolve(keyId);
                    },

                    ValidIssuer = jwtOption.ValidIssuer,
                    ValidAudience = jwtOption.ValidAudiences,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                };
            })
        );
        return builder;
    }

    public static WebApplication UseIAMModServices(this WebApplication app)
    {
        app.UseSession();
        app.UseRouting();
        app.UseCors(AppConst.Default);
        app.UseStaticFiles();
        app.UseRequestLocalization();
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.MapSwagger();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapDefaultEndpoints();
        app.MapControllers();
        app.MapRazorPages();
        app.MapFallbackToFile("index.html");


        using (var scope = app.Services.CreateScope())
        {
            var provider = scope.ServiceProvider;
            var task = InitModule.InitializeAsync(provider);
            task.Wait(new TimeSpan(0, 0, 30));
        }
        return app;
    }

}