using Perigon.PostgreSQL;
using EntityFramework.AppDbFactory;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Perigon.AspNetCore.Abstraction;
using Perigon.AspNetCore.Services;
using Perigon.AspNetCore.Toolkit.Services;
using Share.Implement;

namespace ServiceDefaults;

/// <summary>
/// 应用扩展服务
/// </summary>
public static class FrameworkExtensions
{
    public static IHostApplicationBuilder AddFrameworkServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContext, UserContext>();

        var components = builder.Configuration.GetSection(ComponentOption.ConfigPath)
            .Get<ComponentOption>() ?? new ComponentOption();
        builder.AddOptions();
        builder.AddCache(components);
        builder.AddDbFactory();
        builder.AddDbContext();

        // 注册 JWT 服务（基础 token 操作）
        builder.Services.AddScoped<JwtService>();

        // 注册 SMTP 服务
        builder.Services.AddScoped<SmtpService>();

        return builder;
    }

    /// <summary>
    /// config options
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static IHostApplicationBuilder AddOptions(this IHostApplicationBuilder builder)
    {
        var config = builder.Configuration;
        builder.Services.Configure<ComponentOption>(config.GetSection(ComponentOption.ConfigPath));
        builder.Services.Configure<LoginSecurityPolicyOption>(config.GetSection(LoginSecurityPolicyOption.ConfigPath));
        builder.Services.Configure<JwtOption>(config.GetSection(JwtOption.ConfigPath));
        builder.Services.Configure<CacheOption>(config.GetSection(CacheOption.ConfigPath));

        return builder;
    }

    /// <summary>
    /// 添加数据工厂
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IHostApplicationBuilder AddDbFactory(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<AppDbFactory>();
        return builder;
    }

    /// <summary>
    /// 添加数据库上下文
    /// </summary>
    /// <returns></returns>
    public static IHostApplicationBuilder AddDbContext(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextPool<DefaultDbContext>(options =>
        {
            var connection = builder.Configuration.GetConnectionString(AppConst.Default)
                ?? throw new InvalidOperationException("The default database connection is missing.");
            options.UsePostgres(connection);
        });
        return builder;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="components"></param>
    /// <returns></returns>
    public static IHostApplicationBuilder AddCache(
        this IHostApplicationBuilder builder,
        ComponentOption components
    )
    {
        // 默认支持内存缓存
        builder.Services.AddMemoryCache();

        // 分布式缓存
        if (components.Cache != CacheType.Memory)
        {
            builder.AddRedisDistributedCache(AppConst.Cache);
        }
        // 混合缓存
        var cacheOption = builder
            .Configuration.GetSection(CacheOption.ConfigPath)
            .Get<CacheOption>();
        builder.Services.AddSingleton<IHybridCacheSerializerFactory, RejectUnknownCacheSerializerFactory>();
        builder.Services.AddHybridCache(options =>
        {
            HybridCacheEntryFlags? flags = components.Cache switch
            {
                CacheType.Memory => HybridCacheEntryFlags.DisableDistributedCache,
                CacheType.Redis => HybridCacheEntryFlags.DisableLocalCache,
                _ => HybridCacheEntryFlags.None,
            };

            options.MaximumPayloadBytes = cacheOption?.MaxPayloadBytes ?? (1024 * 1024);
            options.MaximumKeyLength = cacheOption?.MaxKeyLength ?? 1024;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Flags = flags,
                Expiration = TimeSpan.FromMinutes(cacheOption?.Expiration ?? 20),
                LocalCacheExpiration = TimeSpan.FromMinutes(
                    cacheOption?.LocalCacheExpiration ?? 10
                ),
            };
        });

        builder.Services.AddSingleton<CacheService>();
        return builder;
    }

    /// <summary>
    /// 仅在调试特殊异常时使用
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication UseDomainException(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
        AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
        {
            if (eventArgs.Exception is OutOfMemoryException)
            {
                logger.LogError(
                    "=== OutOfMemory: {message}, {stackTrace}",
                    eventArgs.Exception.Message,
                    eventArgs.Exception.StackTrace
                );
            }
            else
            {
                logger.LogError(
                    "=== FirstChanceException: {message}, {stackTrace}",
                    eventArgs.Exception.Message,
                    eventArgs.Exception.StackTrace
                );
            }
        };
        return app;
    }
}
