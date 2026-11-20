using Microsoft.Extensions.Configuration;
using Perigon.PostgreSQL.RawSql;
using Microsoft.Extensions.Hosting;
using Perigon.AspNetCore.Services;
using Share.Constants;

namespace IAMMod.Services;

/// <summary>
/// Initializes IAM data after the PostgreSQL schema is ready.
/// </summary>
public class InitHostService(
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    ILogger<InitHostService> logger
)
{
    /// <summary>
    /// Applies the initial IAM signing key and seed catalog after migrations.
    /// This is also called by MigrationService so the API does not race schema setup.
    /// </summary>
    public Task SeedAsync(DefaultDbContext dbContext, CancellationToken cancellationToken = default) =>
        dbContext.TransactionAsync(ct => SeedInTransactionAsync(dbContext, ct), cancellationToken);

    private async Task SeedInTransactionAsync(DefaultDbContext dbContext, CancellationToken cancellationToken)
    {
        // Serialize first-time initialization and roll back the signing key if any seed fails.
        await dbContext.SqlCommand($"SELECT pg_advisory_xact_lock(hashtextextended({"iam-seed-v1"}, 0))")
            .ExecuteAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var hasActiveKey = await dbContext.SigningKeys
            .Where(k => k.IsActive && k.ActivationDate <= now && (k.ExpirationDate == null || k.ExpirationDate > now))
            .AnyAsync(cancellationToken);
        if (hasActiveKey)
        {
            return;
        }

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
            ActivationDate = now,
            ExpirationDate = now.AddDays(365),
            IsActive = true,
            IsDeleted = false
        };

        await dbContext.SigningKeys.InsertAsync(signingKey, returning: false, cancellationToken: cancellationToken);
        logger.LogInformation("Initial signing key generated: {KeyId}", signingKey.KeyId);
        await SeedOAuthDataAsync(dbContext, cancellationToken);
        await SeedInitialDataAsync(dbContext, cancellationToken);
        await SeedPermissionDataAsync(dbContext, cancellationToken);
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
        var adminEmail = "admin@default.com";
        var normalizedAdminEmail = adminEmail.ToUpperInvariant();
        var adminUser = await FindAdminUserAsync(
            dbContext,
            adminUserName,
            normalizedAdminUserName,
            adminEmail,
            normalizedAdminEmail,
            cancellationToken);

        if (adminUser == null)
        {
            var salt = HashCrypto.BuildSalt();
            adminUser = new User
            {
                UserName = adminUserName,
                NormalizedUserName = normalizedAdminUserName,
                Email = adminEmail,
                NormalizedEmail = normalizedAdminEmail,
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

            await dbContext.Users.InsertAsync(adminUser, returning: false, cancellationToken: cancellationToken);
        }
        else
        {
            var requiresUpdate = false;

            if (!string.Equals(adminUser.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
            {
                adminUser.Email = adminEmail;
                requiresUpdate = true;
            }

            if (!string.Equals(adminUser.NormalizedEmail, normalizedAdminEmail, StringComparison.Ordinal))
            {
                adminUser.NormalizedEmail = normalizedAdminEmail;
                requiresUpdate = true;
            }

            if (!adminUser.EmailConfirmed)
            {
                adminUser.EmailConfirmed = true;
                requiresUpdate = true;
            }

            if (requiresUpdate)
            {
                adminUser.UpdatedTime = DateTimeOffset.UtcNow;
                await dbContext.Users
                    .Where(item => item.Id == adminUser.Id)
                    .ExecuteUpdateAsync(update => update
                        .Set(item => item.Email, adminUser.Email)
                        .Set(item => item.NormalizedEmail, adminUser.NormalizedEmail)
                        .Set(item => item.EmailConfirmed, adminUser.EmailConfirmed)
                        .Set(item => item.UpdatedTime, adminUser.UpdatedTime),
                        cancellationToken: cancellationToken);
            }
        }

        var hasSuperAdminRole = await dbContext.UserRoles.AnyAsync(
            item => item.UserId == adminUser.Id && item.RoleId == superAdminRole.Id,
            cancellationToken);

        if (!hasSuperAdminRole)
        {
            await dbContext.UserRoles.InsertAsync(
                new UserRole { UserId = adminUser.Id, RoleId = superAdminRole.Id },
                returning: false,
                cancellationToken: cancellationToken);
        }
    }

    private async Task<User?> FindAdminUserAsync(
        DefaultDbContext dbContext,
        string adminUserName,
        string normalizedAdminUserName,
        string adminEmail,
        string normalizedAdminEmail,
        CancellationToken cancellationToken)
    {
        var adminUser = await dbContext.Users
            .Where(u => u.NormalizedEmail == normalizedAdminEmail)
            .OrderBy(u => u.CreatedTime)
            .ThenBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminUser != null)
        {
            return adminUser;
        }

        adminUser = await dbContext.Users
            .Where(u => u.Email == adminEmail || (u.Email != null && u.Email.ToUpper() == normalizedAdminEmail))
            .OrderBy(u => u.CreatedTime)
            .ThenBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminUser != null)
        {
            return adminUser;
        }

        var legacyAdminCandidates = await dbContext.Users
            .Where(u => u.NormalizedUserName == normalizedAdminUserName || u.UserName == adminUserName)
            .OrderBy(u => u.CreatedTime)
            .ThenBy(u => u.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (legacyAdminCandidates.Count == 1)
        {
            return legacyAdminCandidates[0];
        }

        if (legacyAdminCandidates.Count > 1)
        {
            logger.LogWarning(
                "Multiple legacy admin candidates matched username {AdminUserName}. A dedicated default admin account will be created using email {AdminEmail} instead of mutating an arbitrary user.",
                adminUserName,
                adminEmail);
        }

        return null;
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

                await dbContext.ApiScopes.InsertAsync(scope, returning: false, cancellationToken: cancellationToken);
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
            await dbContext.ApiResources.InsertAsync(defaultResource, returning: false, cancellationToken: cancellationToken);
        }

        // Get all scopes for client assignment
        var openidScope = await dbContext.ApiScopes.FirstAsync(s => s.Name == "openid", cancellationToken);
        var profileScope = await dbContext.ApiScopes.FirstAsync(s => s.Name == "profile", cancellationToken);
        var emailScope = await dbContext.ApiScopes.FirstAsync(s => s.Name == "email", cancellationToken);
        var offlineAccessScope = await dbContext.ApiScopes.FirstAsync(s => s.Name == "offline_access", cancellationToken);
        var sampleApiScope = await dbContext.ApiScopes.FirstAsync(s => s.Name == "SampleAPI", cancellationToken);

        // Create AdminWebClient for IAM management portal
        var adminWebClientId = "AdminWebClient";
        var adminWebClientRedirectUris = GetAdminWebClientRedirectUris();
        var adminWebClientPostLogoutRedirectUris = GetAdminWebClientPostLogoutRedirectUris();
        var adminWebClientExists = await dbContext.Clients.AnyAsync(
            c => c.ClientIdentifier == adminWebClientId,
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

            await dbContext.Clients.InsertAsync(adminWebClient, returning: false, cancellationToken: cancellationToken);

            var adminWebClientScopes = new[]
            {
                new ClientScope { ClientId = adminWebClient.Id, ScopeId = openidScope.Id },
                new ClientScope { ClientId = adminWebClient.Id, ScopeId = profileScope.Id },
                new ClientScope { ClientId = adminWebClient.Id, ScopeId = emailScope.Id },
                new ClientScope { ClientId = adminWebClient.Id, ScopeId = offlineAccessScope.Id }
            };
            await dbContext.ClientScopes.BulkInsertAsync(adminWebClientScopes, cancellationToken: cancellationToken);
        }
        else
        {
            var adminWebClient = await dbContext.Clients.FirstAsync(c => c.ClientIdentifier == adminWebClientId, cancellationToken);
            var updated = false;

            foreach (var redirectUri in adminWebClientRedirectUris)
            {
                if (!adminWebClient.RedirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
                {
                    adminWebClient.RedirectUriValues = [.. adminWebClient.RedirectUriValues, redirectUri];
                    updated = true;
                }
            }

            foreach (var redirectUri in adminWebClientPostLogoutRedirectUris)
            {
                if (!adminWebClient.PostLogoutRedirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
                {
                    adminWebClient.PostLogoutRedirectUriValues = [.. adminWebClient.PostLogoutRedirectUriValues, redirectUri];
                    updated = true;
                }
            }

            if (updated)
            {
                await dbContext.Clients
                    .Where(item => item.Id == adminWebClient.Id)
                    .ExecuteUpdateAsync(update => update
                        .Set(item => item.RedirectUriValues, adminWebClient.RedirectUriValues)
                        .Set(item => item.PostLogoutRedirectUriValues, adminWebClient.PostLogoutRedirectUriValues),
                        cancellationToken: cancellationToken);
            }

            var existingAdminWebScopeIds = await dbContext.ClientScopes
                .Where(cs => cs.ClientId == adminWebClient.Id)
                .Select(cs => cs.ScopeId)
                .ToScalarListAsync(cancellationToken);

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
                await dbContext.ClientScopes.BulkInsertAsync(missingAdminWebScopes, cancellationToken: cancellationToken);
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
            c => c.ClientIdentifier == frontSampleClientId,
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

            await dbContext.Clients.InsertAsync(frontSampleClient, returning: false, cancellationToken: cancellationToken);

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
            await dbContext.ClientResources.BulkInsertAsync(frontSampleClientResources, cancellationToken: cancellationToken);
            await dbContext.ClientScopes.BulkInsertAsync(frontSampleClientScopes, cancellationToken: cancellationToken);
        }
        else
        {
            var frontSampleClient = await dbContext.Clients.FirstAsync(c => c.ClientIdentifier == frontSampleClientId, cancellationToken);
            var updated = false;

            foreach (var redirectUri in frontSampleClientRedirectUris)
            {
                if (!frontSampleClient.RedirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
                {
                    frontSampleClient.RedirectUriValues = [.. frontSampleClient.RedirectUriValues, redirectUri];
                    updated = true;
                }
            }

            foreach (var redirectUri in frontSampleClientPostLogoutRedirectUris)
            {
                if (!frontSampleClient.PostLogoutRedirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
                {
                    frontSampleClient.PostLogoutRedirectUriValues = [.. frontSampleClient.PostLogoutRedirectUriValues, redirectUri];
                    updated = true;
                }
            }

            if (updated)
            {
                await dbContext.Clients
                    .Where(item => item.Id == frontSampleClient.Id)
                    .ExecuteUpdateAsync(update => update
                        .Set(item => item.RedirectUriValues, frontSampleClient.RedirectUriValues)
                        .Set(item => item.PostLogoutRedirectUriValues, frontSampleClient.PostLogoutRedirectUriValues),
                        cancellationToken: cancellationToken);
            }

            var existingFrontSampleScopeIds = await dbContext.ClientScopes
                .Where(cs => cs.ClientId == frontSampleClient.Id)
                .Select(cs => cs.ScopeId)
                .ToScalarListAsync(cancellationToken);

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
                await dbContext.ClientScopes.BulkInsertAsync(missingFrontSampleScopes, cancellationToken: cancellationToken);
                updated = true;
            }

            var hasDefaultResource = await dbContext.ClientResources.AnyAsync(
                cr => cr.ClientId == frontSampleClient.Id && cr.ApiResourceId == defaultResource.Id,
                cancellationToken);

            if (!hasDefaultResource)
            {
                await dbContext.ClientResources.InsertAsync(new ClientResource
                {
                    ClientId = frontSampleClient.Id,
                    ApiResourceId = defaultResource.Id,
                }, returning: false, cancellationToken: cancellationToken);
                updated = true;
            }

            if (updated)
            {
                // Perigon commands execute immediately.
            }
        }

        // Create ApiClient for backend API services
        var apiClientId = "ApiService";
        var apiClientExists = await dbContext.Set<Client>().AnyAsync(
            c => c.ClientIdentifier == apiClientId,
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

            await dbContext.Clients.InsertAsync(apiClient, returning: false, cancellationToken: cancellationToken);

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

            await dbContext.ClientScopes.BulkInsertAsync(apiClientScopes, cancellationToken: cancellationToken);
            await dbContext.ClientResources.BulkInsertAsync(apiClientResources, cancellationToken: cancellationToken);
        }
        else
        {
            var apiClient = await dbContext.Clients.FirstAsync(c => c.ClientIdentifier == apiClientId, cancellationToken);
            var existingApiClientScopeIds = await dbContext.ClientScopes
                .Where(cs => cs.ClientId == apiClient.Id)
                .Select(cs => cs.ScopeId)
                .ToScalarListAsync(cancellationToken);

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
                await dbContext.ClientScopes.BulkInsertAsync(missingApiClientScopes, cancellationToken: cancellationToken);
            }

            var hasDefaultResource = await dbContext.ClientResources.AnyAsync(
                cr => cr.ClientId == apiClient.Id && cr.ApiResourceId == defaultResource.Id,
                cancellationToken);

            if (!hasDefaultResource)
            {
                await dbContext.ClientResources.InsertAsync(new ClientResource
                {
                    ClientId = apiClient.Id,
                    ApiResourceId = defaultResource.Id,
                }, returning: false, cancellationToken: cancellationToken);
            }
        }

    }

    /// <summary>
    /// Seed the unified permission model and assign default admin permissions.
    /// </summary>
    private async Task SeedPermissionDataAsync(DefaultDbContext dbContext, CancellationToken cancellationToken)
    {
        var adminClient = await dbContext.Clients
            .FirstAsync(item => item.ClientIdentifier == PermissionSeedCatalog.AdminWebClientCode, cancellationToken);

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


        var adminRoleIds = await dbContext.Roles
            .Where(item => item.Name == WebConst.SuperAdmin || item.Name == WebConst.AdminUser)
            .Select(item => item.Id)
            .ToScalarListAsync(cancellationToken);

        var allPermissionIds = await dbContext.Permissions
            .Select(item => item.Id)
            .ToScalarListAsync(cancellationToken);

        foreach (var roleId in adminRoleIds)
        {
            await dbContext.RolePermissions
                .Where(item => item.RoleId == roleId)
                .ExecuteDeleteAsync(cancellationToken: cancellationToken);

            if (allPermissionIds.Count > 0)
            {
                await dbContext.RolePermissions.BulkInsertAsync(allPermissionIds.Select(permissionId => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                }), cancellationToken: cancellationToken);
            }
        }

    }

    private IReadOnlyList<string> GetAdminWebClientRedirectUris()
    {
        if (hostEnvironment.IsDevelopment())
        {
            return new[]
            {
                "http://localhost:4200",
                "https://localhost:4200",
                "http://localhost:4200/auth/callback",
                "https://localhost:4200/auth/callback"
            };
        }

        var baseOrigin = ResolveAdminWebClientBaseOrigin();
        return new[]
        {
            baseOrigin,
            $"{baseOrigin}/auth/callback",
        };
    }

    private IReadOnlyList<string> GetAdminWebClientPostLogoutRedirectUris()
    {
        if (hostEnvironment.IsDevelopment())
        {
            return new[]
            {
                "http://localhost:4200",
                "https://localhost:4200"
            };
        }

        var baseOrigin = ResolveAdminWebClientBaseOrigin();
        return new[] { baseOrigin };
    }

    private string ResolveAdminWebClientBaseOrigin()
    {
        var configuredOrigin = configuration["Authentication:Issuer"]
            ?? configuration["Authentication:PublicOrigin"];

        if (Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var configuredUri))
        {
            return configuredUri.GetLeftPart(UriPartial.Authority);
        }

        var runtimePort = GetRuntimePort(configuration["ASPNETCORE_HTTP_PORTS"])
            ?? GetRuntimePort(configuration["ASPNETCORE_URLS"])
            ?? 8080;

        return $"http://localhost:{runtimePort}";
    }

    private static int? GetRuntimePort(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        foreach (var token in rawValue.Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(token, out var port))
            {
                return port;
            }

            if (Uri.TryCreate(token, UriKind.Absolute, out var uri) && uri.Port > 0)
            {
                return uri.Port;
            }
        }

        return null;
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

        await dbContext.Roles.InsertAsync(role, returning: false, cancellationToken: cancellationToken);
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
        var isNewPermission = false;
        if (!lookup.TryGetValue(seed.Code, out var permission))
        {
            var existingPermission = await dbContext.Permissions
                .FirstOrDefaultAsync(item => item.Code == seed.Code, cancellationToken)
                ;
            permission = existingPermission ?? new Permission { Code = seed.Code, Name = seed.Name };
            isNewPermission = existingPermission is null;

            lookup[seed.Code] = permission;
            if (isNewPermission)
            {
                permission.Id = Guid.CreateVersion7();
                await dbContext.Permissions.InsertAsync(permission, returning: false, cancellationToken);
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
        if (!isNewPermission)
        {
            await dbContext.Permissions
                .Where(item => item.Id == permission.Id)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.Name, permission.Name)
                    .Set(item => item.Type, permission.Type)
                    .Set(item => item.Path, permission.Path)
                    .Set(item => item.ParentId, permission.ParentId)
                    .Set(item => item.OwnedClientId, permission.OwnedClientId)
                    .Set(item => item.IsDeleted, false)
                    .Set(item => item.UpdatedTime, permission.UpdatedTime),
                    cancellationToken: cancellationToken);
        }

        if (attachToClientId.HasValue && !await dbContext.ClientPermissions
            .Where(item => item.ClientId == attachToClientId && item.PermissionId == permission.Id)
            .AnyAsync(cancellationToken))
        {
            await dbContext.ClientPermissions.InsertAsync(new ClientPermission
            {
                ClientId = attachToClientId.Value,
                PermissionId = permission.Id,
            }, returning: false, cancellationToken);
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
