using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Share.Services;

namespace AccessMod;

public static class ModuleExtensions
{
    /// <summary>
    /// 注册 AccessMod 模块服务，包括密钥管理与 OAuth 核心逻辑
    /// </summary>
    public static IHostApplicationBuilder AddAccessMod(this IHostApplicationBuilder builder)
    {
        // 注册 Share 层的 OAuthService
        builder.Services.AddScoped<OAuthService>();
        return builder;
    }

}