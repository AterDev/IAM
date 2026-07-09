using Microsoft.Extensions.Hosting;

namespace UserCenterMod.Services;

/// <summary>
/// module init host service
/// </summary>
public class InitUserCenterModService(ILogger<InitUserCenterModService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("UserCenterMod initializing...");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UserCenterMod initialization failed");
            return Task.CompletedTask;
        }
        finally
        {
        }

        return Task.CompletedTask;
    }
}
