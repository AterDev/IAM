using EntityFramework.AppDbFactory;
using EntityFramework.AppDbContext;
using IAMMod.Services;
using System.Diagnostics;

namespace MigrationService;

public sealed class Worker(
    AppDbFactory dbFactory,
    InitHostService seedService,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<Worker> logger) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("Migrating database", ActivityKind.Client);
        try
        {
            await using var dbContext = dbFactory.CreateDbContext();
            logger.LogInformation("Migrating database with {ContextType}", nameof(DefaultDbContext));
            await dbContext.MigrateAsync(cancellationToken);
            await seedService.SeedAsync(dbContext, cancellationToken);
            logger.LogInformation("Database migration completed.");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            activity?.AddException(exception);
            logger.LogError(exception, "Database migration failed.");
            Environment.ExitCode = 1;
        }
        finally
        {
            hostApplicationLifetime.StopApplication();
        }
    }
}
