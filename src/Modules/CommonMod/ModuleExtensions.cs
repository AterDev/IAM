using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Share.Services;

namespace CommonMod;

public static class ModuleExtensions
{
    /// <summary>
    /// Register CommonMod services
    /// </summary>
    public static IHostApplicationBuilder AddCommonMod(this IHostApplicationBuilder builder)
    {
        // Register cross-module services
        builder.Services.AddScoped<IAuditTrailService, AuditTrailService>();
        builder.Services.AddSingleton<IPasswordHasher, PasswordHasherService>();

        return builder;
    }
}
