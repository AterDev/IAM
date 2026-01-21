using Microsoft.Extensions.Hosting;

namespace IdentityMod;

public static class ModuleExtensions
{
    /// <summary>
    /// module services or init task
    /// </summary>
    public static IHostApplicationBuilder AddIdentityMod(this IHostApplicationBuilder builder)
    {
        return builder;
    }
}
