using System.Diagnostics;
using Entity.IdentityMod;
using Microsoft.EntityFrameworkCore;
using Share.Services;

namespace MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<Worker> logger
) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity(
            "Migrating database",
            ActivityKind.Client
        );
        try
        {
            using var scope = serviceProvider.CreateScope();
            _logger.LogInformation("migrations {db}", nameof(DefaultDbContext));
            var dbContext = scope.ServiceProvider.GetRequiredService<DefaultDbContext>();
            await RunMigrationAsync(dbContext, cancellationToken);
            await SeedDataAsync(dbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
        }
        finally
        {
            hostApplicationLifetime.StopApplication();
        }
    }

    private static async Task RunMigrationAsync<T>(T dbContext, CancellationToken cancellationToken)
        where T : DbContext
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }

    private static async Task SeedDataAsync<T>(T dbContext, CancellationToken cancellationToken)
        where T : DbContext
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            if (dbContext is DefaultDbContext defaultContext)
            {
                await SeedInitialDataAsync(defaultContext, cancellationToken);
            }
        });
    }

    /// <summary>
    /// Seed initial data including default admin account
    /// </summary>
    private static async Task SeedInitialDataAsync(
        DefaultDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        // Check if admin user already exists
        var adminUserName = "admin";
        var normalizedAdminUserName = adminUserName.ToUpperInvariant();

        var adminExists = await dbContext.Users.AnyAsync(
            u => u.NormalizedUserName == normalizedAdminUserName,
            cancellationToken
        );

        if (!adminExists)
        {
            // Create password hasher
            var passwordHasher = new PasswordHasherService();

            // Create default admin role if not exists
            var adminRoleName = "Administrator";
            var normalizedAdminRoleName = adminRoleName.ToUpperInvariant();

            var adminRole = await dbContext.Roles.FirstOrDefaultAsync(
                r => r.NormalizedName == normalizedAdminRoleName,
                cancellationToken
            );

            if (adminRole == null)
            {
                adminRole = new Role
                {
                    Name = adminRoleName,
                    NormalizedName = normalizedAdminRoleName,
                    Description = "System Administrator Role",
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                };

                dbContext.Roles.Add(adminRole);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            // Create admin user
            var adminUser = new User
            {
                UserName = adminUserName,
                NormalizedUserName = normalizedAdminUserName,
                Email = "admin@iam.local",
                NormalizedEmail = "ADMIN@IAM.LOCAL",
                EmailConfirmed = true,
                PasswordHash = passwordHasher.HashPassword("MakeDotnetGreatAgain"),
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                LockoutEnabled = true,
                PhoneNumberConfirmed = false,
                IsTwoFactorEnabled = false,
                AccessFailedCount = 0,
            };

            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Assign admin role to admin user
            var userRole = new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id };

            dbContext.UserRoles.Add(userRole);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
