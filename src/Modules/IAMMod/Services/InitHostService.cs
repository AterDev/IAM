using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Perigon.AspNetCore.Services;
using Share.Constants;

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

            await SeedOAuthDataAsync(dbContext, stoppingToken);
            await SeedInitialDataAsync(dbContext, stoppingToken);
            await SeedPermissionDataAsync(dbContext, stoppingToken);

            var cacheService = scope.ServiceProvider.GetRequiredService<CacheService>();
            await cacheService.RemoveAsync(OAuthConst.SigningActiveKeyCacheKey);
            await cacheService.RemoveAsync(OAuthConst.SigningKeyCacheKey);

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
        var superAdminRole = await EnsureRoleAsync(
            dbContext,
            WebConst.SuperAdmin,
            "System Administrator Role",
            cancellationToken);

        await EnsureRoleAsync(
            dbContext,
            WebConst.AdminUser,
            "System Administrator User Role",
            cancellationToken);

        var adminUserName = "admin";
        var normalizedAdminUserName = adminUserName.ToUpperInvariant();
        var adminUser = await dbContext.Users.FirstOrDefaultAsync(
            u => u.NormalizedUserName == normalizedAdminUserName,
            cancellationToken);

        if (adminUser == null)
        {
            var salt = HashCrypto.BuildSalt();
            adminUser = new User
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
        }

        var hasSuperAdminRole = await dbContext.UserRoles.AnyAsync(
            item => item.UserId == adminUser.Id && item.RoleId == superAdminRole.Id,
            cancellationToken);

        if (!hasSuperAdminRole)
        {
            dbContext.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = superAdminRole.Id });
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
            ("offline_access", "Offline Access", "离线访问权限(刷新令牌)", false),
            ("SampleAPI", "SampleAPI", "示例API访问权限", false)
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
        var defaultResource = await dbContext.ApiResources.FirstOrDefaultAsync(
            r => r.Name == "SampleAPI",
            cancellationToken
        );
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
        var sampleApiScope = await dbContext.ApiScopes.FirstAsync(s => s.Name == "SampleAPI", cancellationToken);

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

            var existingAdminWebScopeIds = await dbContext.ClientScopes
                .Where(cs => cs.ClientId == adminWebClient.Id)
                .Select(cs => cs.ScopeId)
                .ToListAsync(cancellationToken);

            var requiredAdminWebScopeIds = new[]
            {
                openidScope.Id,
                profileScope.Id,
                emailScope.Id,
                offlineAccessScope.Id,
            };

            var missingAdminWebScopes = requiredAdminWebScopeIds
                .Except(existingAdminWebScopeIds)
                .Select(scopeId => new ClientScope { ClientId = adminWebClient.Id, ScopeId = scopeId })
                .ToList();

            if (missingAdminWebScopes.Count > 0)
            {
                dbContext.ClientScopes.AddRange(missingAdminWebScopes);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // Create FrontSampleClient for sample frontend application
        var frontSampleClientId = "FrontSampleClient";
        var frontSampleClientRedirectUris = new[]
        {
            "http://localhost:4201",
            "https://localhost:4201",
            "http://localhost:4201/auth/callback",
            "https://localhost:4201/auth/callback"
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
                new ClientScope { ClientId = frontSampleClient.Id, ScopeId = offlineAccessScope.Id },
                new ClientScope { ClientId = frontSampleClient.Id, ScopeId = sampleApiScope.Id }
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

            var existingFrontSampleScopeIds = await dbContext.ClientScopes
                .Where(cs => cs.ClientId == frontSampleClient.Id)
                .Select(cs => cs.ScopeId)
                .ToListAsync(cancellationToken);

            var requiredFrontSampleScopeIds = new[]
            {
                openidScope.Id,
                profileScope.Id,
                emailScope.Id,
                offlineAccessScope.Id,
                sampleApiScope.Id,
            };

            var missingFrontSampleScopes = requiredFrontSampleScopeIds
                .Except(existingFrontSampleScopeIds)
                .Select(scopeId => new ClientScope { ClientId = frontSampleClient.Id, ScopeId = scopeId })
                .ToList();

            if (missingFrontSampleScopes.Count > 0)
            {
                dbContext.ClientScopes.AddRange(missingFrontSampleScopes);
                updated = true;
            }

            var hasDefaultResource = await dbContext.ClientResources.AnyAsync(
                cr => cr.ClientId == frontSampleClient.Id && cr.ApiResourceId == defaultResource.Id,
                cancellationToken);

            if (!hasDefaultResource)
            {
                dbContext.ClientResources.Add(new ClientResource
                {
                    ClientId = frontSampleClient.Id,
                    ApiResourceId = defaultResource.Id,
                });
                updated = true;
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
                new ClientScope { ClientId = apiClient.Id, ScopeId = openidScope.Id },
                new ClientScope { ClientId = apiClient.Id, ScopeId = sampleApiScope.Id }
            };

            var apiClientResources = new[]
            {
                new ClientResource { ClientId = apiClient.Id, ApiResourceId = defaultResource.Id }
            };

            dbContext.ClientScopes.AddRange(apiClientScopes);
            dbContext.ClientResources.AddRange(apiClientResources);
        }
        else
        {
            var apiClient = await dbContext.Clients.FirstAsync(c => c.ClientId == apiClientId, cancellationToken);
            var existingApiClientScopeIds = await dbContext.ClientScopes
                .Where(cs => cs.ClientId == apiClient.Id)
                .Select(cs => cs.ScopeId)
                .ToListAsync(cancellationToken);

            var requiredApiClientScopeIds = new[]
            {
                openidScope.Id,
                sampleApiScope.Id,
            };

            var missingApiClientScopes = requiredApiClientScopeIds
                .Except(existingApiClientScopeIds)
                .Select(scopeId => new ClientScope { ClientId = apiClient.Id, ScopeId = scopeId })
                .ToList();

            if (missingApiClientScopes.Count > 0)
            {
                dbContext.ClientScopes.AddRange(missingApiClientScopes);
            }

            var hasDefaultResource = await dbContext.ClientResources.AnyAsync(
                cr => cr.ClientId == apiClient.Id && cr.ApiResourceId == defaultResource.Id,
                cancellationToken);

            if (!hasDefaultResource)
            {
                dbContext.ClientResources.Add(new ClientResource
                {
                    ClientId = apiClient.Id,
                    ApiResourceId = defaultResource.Id,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Seed the unified permission model and assign default admin permissions.
    /// </summary>
    private async Task SeedPermissionDataAsync(DefaultDbContext dbContext, CancellationToken cancellationToken)
    {
        var adminClient = await dbContext.Clients
            .FirstAsync(item => item.ClientId == PermissionSeedCatalog.AdminWebClientCode, cancellationToken);

        var seededPermissions = new Dictionary<string, Permission>(StringComparer.Ordinal);

        foreach (var rootSeed in PermissionSeedCatalog.AdminWebMenuPermissions)
        {
            await UpsertPermissionSeedAsync(
                dbContext,
                seed: rootSeed,
                ownerClientId: adminClient.Id,
                attachToClientId: adminClient.Id,
                parent: null,
                lookup: seededPermissions,
                cancellationToken: cancellationToken);
        }

        foreach (var businessSeed in PermissionSeedCatalog.DefaultBusinessPermissions)
        {
            await UpsertPermissionSeedAsync(
                dbContext,
                seed: businessSeed,
                ownerClientId: null,
                attachToClientId: adminClient.Id,
                parent: null,
                lookup: seededPermissions,
                cancellationToken: cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var adminRoleIds = await dbContext.Roles
            .Where(item => item.Name == WebConst.SuperAdmin || item.Name == WebConst.AdminUser)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        var allPermissionIds = await dbContext.Permissions
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        foreach (var roleId in adminRoleIds)
        {
            await dbContext.RolePermissions
                .Where(item => item.RoleId == roleId)
                .ExecuteDeleteAsync(cancellationToken);

            if (allPermissionIds.Count > 0)
            {
                dbContext.RolePermissions.AddRange(allPermissionIds.Select(permissionId => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                }));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Role> EnsureRoleAsync(
        DefaultDbContext dbContext,
        string roleName,
        string description,
        CancellationToken cancellationToken)
    {
        var normalizedRoleName = roleName.ToUpperInvariant();
        var role = await dbContext.Roles.FirstOrDefaultAsync(
            item => item.NormalizedName == normalizedRoleName,
            cancellationToken);

        if (role != null)
        {
            return role;
        }

        role = new Role
        {
            Name = roleName,
            NormalizedName = normalizedRoleName,
            Description = description,
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };

        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);
        return role;
    }

    private static async Task<Permission> UpsertPermissionSeedAsync(
        DefaultDbContext dbContext,
        PermissionSeedDefinition seed,
        Guid? ownerClientId,
        Guid? attachToClientId,
        Permission? parent,
        Dictionary<string, Permission> lookup,
        CancellationToken cancellationToken)
    {
        if (!lookup.TryGetValue(seed.Code, out var permission))
        {
            permission = await dbContext.Permissions
                .Include(item => item.ClientPermissions)
                .FirstOrDefaultAsync(item => item.Code == seed.Code, cancellationToken)
                ?? new Permission { Code = seed.Code, Name = seed.Name };

            lookup[seed.Code] = permission;
            if (permission.Id == Guid.Empty)
            {
                permission.Id = Guid.CreateVersion7();
            }

            if (dbContext.Entry(permission).State == EntityState.Detached)
            {
                dbContext.Permissions.Add(permission);
            }
        }

        permission.Name = seed.Name;
        permission.Type = seed.Type;
        permission.Path = seed.Path;
        permission.Parent = parent;
        permission.ParentId = parent?.Id;
        permission.OwnedClientId = ownerClientId;
        permission.UpdatedTime = DateTimeOffset.UtcNow;
        permission.IsDeleted = false;

        if (attachToClientId.HasValue && !permission.ClientPermissions.Any(item => item.ClientId == attachToClientId.Value))
        {
            permission.ClientPermissions.Add(new ClientPermission
            {
                ClientId = attachToClientId.Value,
                Permission = permission,
            });
        }

        foreach (var child in seed.Children)
        {
            await UpsertPermissionSeedAsync(
                dbContext,
                child,
                ownerClientId,
                attachToClientId,
                permission,
                lookup,
                cancellationToken);
        }

        return permission;
    }
}
