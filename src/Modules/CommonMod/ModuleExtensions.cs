using Microsoft.Extensions.Hosting;

namespace CommonMod;

public static class ModuleExtensions
{
    /// <summary>
    /// Register CommonMod services
    /// </summary>
    public static IHostApplicationBuilder AddCommonMod(this IHostApplicationBuilder builder)
    {
        return builder;
    }
}
