using IdentityMod.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IdentityMod;

public static class ModuleExtensions
{
    /// <summary>
    /// module services or init task
    /// </summary>
    public static IHostApplicationBuilder AddIdentityModMod(this IHostApplicationBuilder builder)
    {
        // Register managers
        builder.Services.AddScoped<AuthorizationManager>();
        builder.Services.AddScoped<TokenManager>();
        builder.Services.AddScoped<DeviceFlowManager>();

        return builder;
    }
}