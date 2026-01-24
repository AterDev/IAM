using Entity.IAMMod;
using Microsoft.EntityFrameworkCore;
using Perigon.AspNetCore.Constants;
using Perigon.AspNetCore.Utils;
using System.Diagnostics;

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
                await SeedOAuthDataAsync(defaultContext, cancellationToken);
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
            // Create default admin role if not exists
            var adminRoleName = WebConst.SuperAdmin;
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
            var salt = HashCrypto.BuildSalt();
            var adminUser = new User
            {
                UserName = adminUserName,
                NormalizedUserName = normalizedAdminUserName,
                Email = "admin@default.com",
                NormalizedEmail = "ADMIN@DEFAULT.COM",
                EmailConfirmed = true,
                PasswordSalt = salt,
                PasswordHash = HashCrypto.GeneratePwd("Perigon.2026", salt),
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

    /// <summary>
    /// Seed OAuth/OIDC initial data including default clients and scopes
    /// </summary>
    private static async Task SeedOAuthDataAsync(
        DefaultDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        // Create default scopes
        var defaultScopes = new List<(string Name, string DisplayName, string Description, bool Required)>
        {
            ("openid", "OpenID", "OpenID Connect身份认证", true),
            ("profile", "Profile", "用户基本信息", false),
            ("email", "Email", "用户邮箱地址", false),
            ("offline_access", "Offline Access", "离线访问权限(刷新令牌)", false)
        };

        foreach (var (name, displayName, description, required) in defaultScopes)
        {
            var scopeExists = await dbContext.ApiScopes.AnyAsync(
                s => s.Name == name,
                cancellationToken
            );

            if (!scopeExists)
            {
                var scope = new ApiScope
                {
                    Name = name,
                    DisplayName = displayName,
                    Description = description,
                    Required = required,
                    Emphasize = required
                };

                dbContext.ApiScopes.Add(scope);
            }
        }

        // add dfault API resource
        var defaultResource = await dbContext.ApiResources.FirstOrDefaultAsync();
        if (defaultResource == null)
        {

            defaultResource = new ApiResource
            {
                Name = "SampleAPI",
                DisplayName = "SampleAPI",
                Description = "示例API资源",
            };
            dbContext.ApiResources.Add(defaultResource);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Get all scopes for client assignment
        var openidScope = await dbContext.ApiScopes.FirstAsync(s => s.Name == "openid", cancellationToken);
        var profileScope = await dbContext.ApiScopes.FirstAsync(s => s.Name == "profile", cancellationToken);
        var emailScope = await dbContext.ApiScopes.FirstAsync(s => s.Name == "email", cancellationToken);
        var offlineAccessScope = await dbContext.ApiScopes.FirstAsync(s => s.Name == "offline_access", cancellationToken);

        // Create FrontClient for frontend applications
        var frontClientId = "FrontClient";
        var frontClientExists = await dbContext.Clients.AnyAsync(
            c => c.ClientId == frontClientId,
            cancellationToken
        );

        if (!frontClientExists)
        {
            var frontClient = new Client
            {
                ClientId = frontClientId,
                DisplayName = "前端客户端",
                Description = "默认的前端单页应用客户端，支持OIDC授权码流程+PKCE",
                Type = ClientType.Public,
                ApplicationType = ApplicationType.Spa,
                RequirePkce = true,
                ConsentType = ConsentType.Implicit,
                RedirectUris =
                [
                    "http://localhost:4200",
                    "https://localhost:4200",
                    "http://localhost:4201",
                    "https://localhost:4201"
                ],
                PostLogoutRedirectUris =
                [
                    "http://localhost:4200",
                    "https://localhost:4200",
                    "http://localhost:4201",
                    "https://localhost:4201"
                ],
            };

            dbContext.Clients.Add(frontClient);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Assign scopes to FrontClient
            var frontClientScopes = new[]
            {
                new ClientScope { ClientId = frontClient.Id, ScopeId = openidScope.Id },
                new ClientScope { ClientId = frontClient.Id, ScopeId = profileScope.Id },
                new ClientScope { ClientId = frontClient.Id, ScopeId = emailScope.Id },
                new ClientScope { ClientId = frontClient.Id, ScopeId = offlineAccessScope.Id }
            };
            var frontClientResources = new[]
            {
                new ClientResource { ClientId = frontClient.Id, ApiResourceId = defaultResource.Id }
            };
            dbContext.ClientResources.AddRange(frontClientResources);
            dbContext.ClientScopes.AddRange(frontClientScopes);
        }

        // Create ApiClient for backend API services
        var apiClientId = "ApiClient";
        var apiClientExists = await dbContext.Set<Client>().AnyAsync(
            c => c.ClientId == apiClientId,
            cancellationToken
        );

        if (!apiClientExists)
        {
            var apiClientSecret = "ApiClient_Secret_2025";
            var salt = HashCrypto.BuildSalt();
            var apiClient = new Client
            {
                ClientId = apiClientId,
                SecretSalt = salt,
                SecretHash = HashCrypto.GeneratePwd(apiClientSecret, salt),
                DisplayName = "API客户端",
                Description = "默认的后端API服务客户端，支持客户端凭证流程",
                Type = ClientType.Confidential,
                ApplicationType = ApplicationType.Web,
                RequirePkce = false,
                ConsentType = ConsentType.Implicit,
            };

            dbContext.Set<Client>().Add(apiClient);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Assign scopes to ApiClient
            var apiClientScopes = new[]
            {
                new ClientScope { ClientId = apiClient.Id, ScopeId = openidScope.Id }
            };

            dbContext.ClientScopes.AddRange(apiClientScopes);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
