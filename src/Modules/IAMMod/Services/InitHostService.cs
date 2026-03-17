using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IAMMod.Services;

/// <summary>
/// Application initialization hosted service that runs data seeding and setup
/// </summary>
public class InitHostService(
    IServiceProvider serviceProvider,
    ILogger<InitHostService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DefaultDbContext>();

        try
        {
            logger.LogInformation("Starting application initialization...");

            var now = DateTimeOffset.UtcNow;
            var hasActiveKey = await dbContext.SigningKeys
                .AnyAsync(k => k.IsActive && k.ActivationDate <= now && (k.ExpirationDate == null || k.ExpirationDate > now), stoppingToken);

            if (!hasActiveKey)
            {
                logger.LogInformation("No active signing key found, generating initial key...");
                var (publicKey, privateKey) = HashCrypto.GenerateRsaKeyPair(2048);
                var signingKey = new SigningKey
                {
                    KeyId = Guid.CreateVersion7().ToString(),
                    Algorithm = "RS256",
                    KeyType = "RSA",
                    PrivateKey = privateKey,
                    PublicKey = publicKey,
                    Usage = "signing",
                    ActivationDate = DateTimeOffset.UtcNow,
                    ExpirationDate = DateTimeOffset.UtcNow.AddDays(365),
                    IsActive = true,
                    IsDeleted = false
                };

                dbContext.SigningKeys.Add(signingKey);
                await dbContext.SaveChangesAsync(stoppingToken);

                logger.LogInformation("Initial signing key generated: {KeyId}", signingKey.KeyId);
            }

            await SeedInitialDataAsync(dbContext, stoppingToken);
            await SeedOAuthDataAsync(dbContext, stoppingToken);

            // Preload signing keys into cache for JWT validation
            var signingKeyResolver = scope.ServiceProvider.GetRequiredService<SigningKeyResolver>();
            await signingKeyResolver.PreloadSigningKeysAsync();

            logger.LogInformation("Application initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Application initialization failed");
            return;
        }
        finally
        {
        }
    }

    /// <summary>
    /// Seed initial data including default admin account
    /// </summary>
    private async Task SeedInitialDataAsync(
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

            var currentAdminPermissions = await dbContext.RoleClaims
                .Where(rc => rc.RoleId == adminRole.Id && rc.ClaimType == PermissionsConst.ClaimType)
                .Select(rc => rc.ClaimValue!)
                .ToListAsync(cancellationToken);

            var missingAdminPermissions = PermissionsConst.All
                .Except(currentAdminPermissions, StringComparer.Ordinal)
                .Select(permission => new RoleClaim
                {
                    RoleId = adminRole.Id,
                    ClaimType = PermissionsConst.ClaimType,
                    ClaimValue = permission,
                })
                .ToList();

            if (missingAdminPermissions.Count > 0)
            {
                dbContext.RoleClaims.AddRange(missingAdminPermissions);
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
    private async Task SeedOAuthDataAsync(
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

        // add default API resource
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

        // Create AdminWebClient for IAM management portal
        var adminWebClientId = "AdminWebClient";
        var adminWebClientRedirectUris = new[]
        {
            "http://localhost:4200",
            "https://localhost:4200",
            "http://localhost:4200/auth/callback",
            "https://localhost:4200/auth/callback"
        };
        var adminWebClientPostLogoutRedirectUris = new[]
        {
            "http://localhost:4200",
            "https://localhost:4200"
        };
        var adminWebClientExists = await dbContext.Clients.AnyAsync(
            c => c.ClientId == adminWebClientId,
            cancellationToken
        );

        if (!adminWebClientExists)
        {
            var adminWebClient = new Client
            {
                ClientId = adminWebClientId,
                DisplayName = "管理后台客户端",
                Description = "IAM 管理后台专用单页应用客户端，支持OIDC授权码流程+PKCE",
                Type = ClientType.Public,
                ApplicationType = ApplicationType.Spa,
                RequirePkce = true,
                ConsentType = ConsentType.Implicit,
                RedirectUris = [.. adminWebClientRedirectUris],
                PostLogoutRedirectUris = [.. adminWebClientPostLogoutRedirectUris],
            };

            dbContext.Clients.Add(adminWebClient);
            await dbContext.SaveChangesAsync(cancellationToken);

            var adminWebClientScopes = new[]
            {
                new ClientScope { ClientId = adminWebClient.Id, ScopeId = openidScope.Id },
                new ClientScope { ClientId = adminWebClient.Id, ScopeId = profileScope.Id },
                new ClientScope { ClientId = adminWebClient.Id, ScopeId = emailScope.Id },
                new ClientScope { ClientId = adminWebClient.Id, ScopeId = offlineAccessScope.Id }
            };
            dbContext.ClientScopes.AddRange(adminWebClientScopes);
        }
        else
        {
            var adminWebClient = await dbContext.Clients.FirstAsync(c => c.ClientId == adminWebClientId, cancellationToken);
            var updated = false;

            foreach (var redirectUri in adminWebClientRedirectUris)
            {
                if (!adminWebClient.RedirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
                {
                    adminWebClient.RedirectUris.Add(redirectUri);
                    updated = true;
                }
            }

            foreach (var redirectUri in adminWebClientPostLogoutRedirectUris)
            {
                if (!adminWebClient.PostLogoutRedirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
                {
                    adminWebClient.PostLogoutRedirectUris.Add(redirectUri);
                    updated = true;
                }
            }

            if (updated)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // Create FrontSampleClient for sample frontend application
        var frontSampleClientId = "FrontSampleClient";
        var frontSampleClientRedirectUris = new[]
        {
            "http://localhost:4201",
            "https://localhost:4201"
        };
        var frontSampleClientPostLogoutRedirectUris = new[]
        {
            "http://localhost:4201",
            "https://localhost:4201"
        };
        var frontSampleClientExists = await dbContext.Clients.AnyAsync(
            c => c.ClientId == frontSampleClientId,
            cancellationToken
        );

        if (!frontSampleClientExists)
        {
            var frontSampleClient = new Client
            {
                ClientId = frontSampleClientId,
                DisplayName = "示例前端客户端",
                Description = "示例前端单页应用客户端，支持OIDC授权码流程+PKCE",
                Type = ClientType.Public,
                ApplicationType = ApplicationType.Spa,
                RequirePkce = true,
                ConsentType = ConsentType.Implicit,
                RedirectUris = [.. frontSampleClientRedirectUris],
                PostLogoutRedirectUris = [.. frontSampleClientPostLogoutRedirectUris],
            };

            dbContext.Clients.Add(frontSampleClient);
            await dbContext.SaveChangesAsync(cancellationToken);

            var frontSampleClientScopes = new[]
            {
                new ClientScope { ClientId = frontSampleClient.Id, ScopeId = openidScope.Id },
                new ClientScope { ClientId = frontSampleClient.Id, ScopeId = profileScope.Id },
                new ClientScope { ClientId = frontSampleClient.Id, ScopeId = emailScope.Id },
                new ClientScope { ClientId = frontSampleClient.Id, ScopeId = offlineAccessScope.Id }
            };
            var frontSampleClientResources = new[]
            {
                new ClientResource { ClientId = frontSampleClient.Id, ApiResourceId = defaultResource.Id }
            };
            dbContext.ClientResources.AddRange(frontSampleClientResources);
            dbContext.ClientScopes.AddRange(frontSampleClientScopes);
        }
        else
        {
            var frontSampleClient = await dbContext.Clients.FirstAsync(c => c.ClientId == frontSampleClientId, cancellationToken);
            var updated = false;

            foreach (var redirectUri in frontSampleClientRedirectUris)
            {
                if (!frontSampleClient.RedirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
                {
                    frontSampleClient.RedirectUris.Add(redirectUri);
                    updated = true;
                }
            }

            foreach (var redirectUri in frontSampleClientPostLogoutRedirectUris)
            {
                if (!frontSampleClient.PostLogoutRedirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
                {
                    frontSampleClient.PostLogoutRedirectUris.Add(redirectUri);
                    updated = true;
                }
            }

            if (updated)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // Create ApiClient for backend API services
        var apiClientId = "ApiService";
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

            dbContext.Clients.Add(apiClient);
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
