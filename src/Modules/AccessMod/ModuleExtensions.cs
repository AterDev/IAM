using Microsoft.Extensions.Hosting;
namespace AccessModMod;

public static class ModuleExtensions
{
    /// <summary>
    /// module services or init task
    /// </summary>
    public static IHostApplicationBuilder AddAccessModMod(this IHostApplicationBuilder builder)
    {
        return builder;
    }
}