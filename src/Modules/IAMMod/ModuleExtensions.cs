using IAMMod.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IAMMod;

public static class ModuleExtensions
{
    /// <summary>
    /// 注册 IAMMod 模块服务，包括密钥管理与 OAuth 核心逻辑
    /// </summary>
    public static IHostApplicationBuilder AddIAMMod(this IHostApplicationBuilder builder)
    {
        // 注册 Share 层的 OAuthService
        builder.Services.AddScoped<OAuthService>();
        return builder;
    }

}