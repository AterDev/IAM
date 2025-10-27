using Microsoft.Extensions.Hosting;
namespace IdentityModMod;

public static class ModuleExtensions
{
    /// <summary>
    /// module services or init task
    /// </summary>
    public static IHostApplicationBuilder AddIdentityModMod(this IHostApplicationBuilder builder)
    {
        return builder;
    }
}