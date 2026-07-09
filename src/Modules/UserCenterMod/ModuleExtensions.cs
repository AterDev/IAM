using Microsoft.Extensions.Hosting;
namespace UserCenterMod;

[DisplayName("Perigon::UserCenterMod")]
[Description("用户中心模块")]
public static class ModuleExtensions
{
    /// <summary>
    /// module services or init task
    /// </summary>
    public static IHostApplicationBuilder AddUserCenterMod(this IHostApplicationBuilder builder)
    {
        builder.AddModServices();
        return builder;
    }

    // The module services registration
    private static IHostApplicationBuilder AddModServices(this IHostApplicationBuilder builder)
    {
        // custom services registration
        return builder;
    }

    // The module middlewares registration
    public static WebApplication UseUserCenterModServices(this WebApplication app)
    {
       // custom middlewares and init task
       return app;
    }
}
