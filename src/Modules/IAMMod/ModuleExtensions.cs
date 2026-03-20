using IAMMod.Managers;
using IAMMod.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceDefaults.Middleware;
using Share.Constants;
using System.ComponentModel;
using System.Net;
using SysClaimTypes = System.Security.Claims.ClaimTypes;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.RateLimiting;

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
        builder.Services.AddScoped<OAuthService>();
        builder.Services.AddScoped<PermissionManager>();
        builder.Services.AddHostedService<InitHostService>();
        builder.AddModServices();
        return builder;
    }

    private static IHostApplicationBuilder AddModServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSwagger();
        builder.Services.Configure<RiskControlOption>(builder.Configuration.GetSection(RiskControlOption.ConfigPath));
        builder.Services.AddScoped<RiskControlService>();
        builder.Services.AddScoped<MfaTotpService>();
        builder.Services.AddCors(options =>
        {
            var origins = builder.Configuration.GetSection("Cors").GetValue<string[]>("AllowedOrigins") ?? [];
            var allowWildcardSubdomains = builder.Configuration.GetSection("Cors").GetValue<bool>("AllowedSubdomains");

            options.AddPolicy(AppConst.Default, policy =>
            {
                if (builder.Environment.IsDevelopment() || origins.Length == 0)
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    return;
                }

                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
                if (allowWildcardSubdomains)
                {
                    policy.SetIsOriginAllowedToAllowWildcardSubdomains();
                }
            });
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(WebConst.TokenEndpoint, context =>
            {
                var remoteIpAddress = context.Connection.RemoteIpAddress;
                if (remoteIpAddress == null || IPAddress.IsLoopback(remoteIpAddress))
                {
                    return RateLimitPartition.GetNoLimiter("loopback-token");
                }

                var partitionKey = remoteIpAddress.ToString();

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromSeconds(30),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    });
            });

            options.AddPolicy(WebConst.DeviceEndpoint, context =>
            {
                var remoteIpAddress = context.Connection.RemoteIpAddress;
                if (remoteIpAddress == null || IPAddress.IsLoopback(remoteIpAddress))
                {
                    return RateLimitPartition.GetNoLimiter("loopback-device");
                }

                var partitionKey = remoteIpAddress.ToString();

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromSeconds(30),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    });
            });
        });

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = builder.Environment.IsProduction()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
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
                options.Cookie.SecurePolicy = builder.Environment.IsProduction()
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = async context =>
                    {
                        var sid = context.Principal?.FindFirst("sid")?.Value;
                        var userIdClaim = context.Principal?.FindFirst(SysClaimTypes.NameIdentifier)?.Value;
                        if (string.IsNullOrWhiteSpace(sid) || !Guid.TryParse(userIdClaim, out var userId))
                        {
                            return;
                        }

                        var sessionManager = context.HttpContext.RequestServices.GetRequiredService<SessionManager>();
                        var valid = await sessionManager.ValidateSessionAsync(userId, sid);
                        if (!valid)
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                    }
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(WebConst.AdminUser, policy =>
            {
                policy.RequireRole(WebConst.AdminUser, WebConst.SuperAdmin);
            });
        builder.Services.AddLocalizer();
        builder.Services.AddThirdAuthentication(builder.Configuration);
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
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var sid = context.Principal?.FindFirst("sid")?.Value;
                        var userIdClaim = context.Principal?.FindFirst(SysClaimTypes.NameIdentifier)?.Value
                            ?? context.Principal?.FindFirst(OAuthConst.JwtClaimNames.Subject)?.Value;

                        if (string.IsNullOrWhiteSpace(sid) || !Guid.TryParse(userIdClaim, out var userId))
                        {
                            return;
                        }

                        var sessionManager = context.HttpContext.RequestServices.GetRequiredService<SessionManager>();
                        var valid = await sessionManager.ValidateSessionAsync(userId, sid);
                        if (!valid)
                        {
                            context.Fail("session_revoked");
                        }
                    },
                    OnChallenge = async context =>
                    {
                        if (!string.Equals(context.AuthenticateFailure?.Message, "session_revoked", StringComparison.Ordinal))
                        {
                            return;
                        }

                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(
                                new
                                {
                                    title = "Unauthorized",
                                    detail = "session_revoked",
                                    status = StatusCodes.Status401Unauthorized,
                                    traceId = context.HttpContext.TraceIdentifier,
                                }
                            )
                        );
                    }
                };
            })
        );
        return builder;
    }

    public static WebApplication UseIAMModServices(this WebApplication app)
    {
        app.UseSession();
        app.UseRouting();

        if (app.Environment.IsProduction())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseCors(AppConst.Default);
        app.UseRateLimiter();
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
        return app;
    }
}