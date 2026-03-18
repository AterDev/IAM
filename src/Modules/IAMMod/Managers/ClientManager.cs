using IAMMod.Models.AuthorizationDtos;
using IAMMod.Models.ClientDtos;
using IAMMod.Models.ResourceDtos;
using IAMMod.Models.ScopeDtos;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text.Json;
using Share;
using Share.Exceptions;

namespace IAMMod.Managers;

/// <summary>
/// Manager for OAuth/OIDC client operations
/// </summary>
public class ClientManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<ClientManager> logger,
    AuditLogManager auditLogManager
) : ManagerBase<DefaultDbContext, Client>(dbContextFactory, userContext, logger)
{
    private const int DefaultSecretExpirationDays = 180;
    private readonly AuditLogManager _auditLogManager = auditLogManager;

    /// <summary>
    /// Get paged clients
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of clients</returns>
    public async Task<PageList<ClientItemDto>> GetPageAsync(ClientFilterDto filter)
    {
        Queryable = Queryable
            .WhereNotNull(filter.ClientId, q => q.ClientId.Contains(filter.ClientId!))
            .WhereNotNull(filter.DisplayName, q => q.DisplayName.Contains(filter.DisplayName!))
            .WhereNotNull(filter.Type, q => q.Type == filter.Type)
            .WhereNotNull(filter.ApplicationType, q => q.ApplicationType == filter.ApplicationType);

        return await PageListAsync<ClientFilterDto, ClientItemDto>(filter);
    }

    /// <summary>
    /// Check if user has permission to access client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        if (_userContext.IsAdmin)
        {
            return true;
        }

        return await _dbSet.AnyAsync(q => q.Id == id && q.DeveloperUserId == _userContext.UserId);
    }

    /// <summary>
    /// Get client detail by id
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>Client detail or null</returns>
    public async Task<ClientDetailDto?> GetDetailAsync(Guid id)
    {
        var client = await Queryable
            .Include(c => c.ClientScopes)
                .ThenInclude(cs => cs.Scope)
            .Include(c => c.ClientResources)
                .ThenInclude(cr => cr.ApiResource)
            .Include(c => c.ClientSecrets)
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();

        if (client == null)
        {
            return null;
        }

        return new ClientDetailDto
        {
            Id = client.Id,
            ClientId = client.ClientId,
            DisplayName = client.DisplayName,
            Description = client.Description,
            Type = client.Type,
            RequirePkce = client.RequirePkce,
            ConsentType = client.ConsentType,
            ApplicationType = client.ApplicationType,
            RegistrationStatus = client.RegistrationStatus,
            DeveloperUserId = client.DeveloperUserId,
            RequestedTime = client.RequestedTime,
            ReviewedTime = client.ReviewedTime,
            ReviewedBy = client.ReviewedBy,
            SecretExpiresAt = client.SecretExpiresAt,
            AllowPasswordGrant = client.AllowPasswordGrant,
            PasswordGrantRestrictionReason = client.PasswordGrantRestrictionReason,
            RedirectUris = client.RedirectUris,
            PostLogoutRedirectUris = client.PostLogoutRedirectUris,
            Scopes = client.ClientScopes.Select(cs => new ScopeItemDto
            {
                Id = cs.Scope.Id,
                Name = cs.Scope.Name,
                DisplayName = cs.Scope.DisplayName,
                Required = cs.Scope.Required,
                CreatedTime = cs.Scope.CreatedTime
            }).ToList(),
            Resources = client.ClientResources.Select(cr => new ClientResourceDto
            {
                Id = cr.ApiResource.Id,
                Name = cr.ApiResource.Name,
                DisplayName = cr.ApiResource.DisplayName,
                Description = cr.ApiResource.Description,
                CreatedTime = cr.ApiResource.CreatedTime
            }).ToList(),
            Secrets = client.ClientSecrets
                .OrderByDescending(s => s.CreatedTime)
                .Select(s => new ClientSecretHistoryDto
                {
                    Id = s.Id,
                    LastFour = s.LastFour,
                    IssuedTime = s.CreatedTime,
                    ExpiresAt = s.ExpiresAt,
                    RevokedAt = s.RevokedAt,
                    IsActive = !s.RevokedAt.HasValue && (!s.ExpiresAt.HasValue || s.ExpiresAt > DateTimeOffset.UtcNow),
                })
                .ToList(),
            CreatedTime = client.CreatedTime,
            UpdatedTime = client.UpdatedTime
        };
    }

    /// <summary>
    /// Add new client
    /// </summary>
    /// <param name="dto">Client add DTO</param>
    /// <returns>Created client detail with secret or null</returns>
    public async Task<string?> AddAsync(ClientAddDto dto)
    {
        if (await _dbSet.AnyAsync(q => q.ClientId == dto.ClientId))
        {
            throw new BusinessException(Localizer.EntityNotFound, StatusCodes.Status400BadRequest);
        }

        var entity = dto.MapTo<Client>();
        entity.RegistrationStatus = ClientRegistrationStatus.Approved;
    NormalizePasswordGrantPolicy(entity);
        var issuedSecret = IssueClientSecret(entity, DefaultSecretExpirationDays);

        return await ExecuteInTransactionAsync(async () =>
        {
            await ApplyScopesAndResourcesAsync(entity, dto.ScopeIds, dto.ResourceIds);
            await InsertAsync(entity);

            await _auditLogManager.AddAuditLogAsync(
                category: "OAuth",
                eventName: "ClientCreated",
                subjectId: entity.Id.ToString(),
                payload: JsonSerializer.Serialize(new
                {
                    entity.ClientId,
                    entity.DisplayName,
                    entity.RegistrationStatus,
                    entity.AllowPasswordGrant,
                    entity.PasswordGrantRestrictionReason,
                })
            );

            return issuedSecret;
        });
    }

    /// <summary>
    /// Self-service client registration request.
    /// </summary>
    public async Task<ClientRegistrationResultDto> RegisterAsync(ClientRegistrationRequestDto dto)
    {
        if (_userContext.UserId == Guid.Empty)
        {
            throw new BusinessException("Forbidden", StatusCodes.Status403Forbidden);
        }

        if (await _dbSet.AnyAsync(q => q.ClientId == dto.ClientId))
        {
            throw new BusinessException(Localizer.EntityNotFound, StatusCodes.Status400BadRequest);
        }

        var entity = dto.MapTo<Client>();
        entity.RegistrationStatus = ClientRegistrationStatus.Pending;
        entity.DeveloperUserId = _userContext.UserId;
        entity.RequestedTime = DateTimeOffset.UtcNow;
    NormalizePasswordGrantPolicy(entity);

        return await ExecuteInTransactionAsync(async () =>
        {
            await ApplyScopesAndResourcesAsync(entity, dto.ScopeIds, dto.ResourceIds);
            await InsertAsync(entity);

            await _auditLogManager.AddAuditLogAsync(
                category: "OAuth",
                eventName: "ClientRegistrationRequested",
                subjectId: entity.Id.ToString(),
                payload: JsonSerializer.Serialize(new
                {
                    entity.ClientId,
                    entity.DisplayName,
                    entity.DeveloperUserId,
                    entity.AllowPasswordGrant,
                    entity.PasswordGrantRestrictionReason,
                })
            );

            return new ClientRegistrationResultDto
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                RegistrationStatus = entity.RegistrationStatus,
                Message = "Client registration submitted for review.",
            };
        });
    }

    /// <summary>
    /// Approve a previously requested client registration.
    /// </summary>
    public async Task<ClientRegistrationResultDto> ApproveAsync(Guid id, int secretExpirationDays)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException(Localizer.ClientNotFound, StatusCodes.Status404NotFound);
        }

        if (entity.RegistrationStatus == ClientRegistrationStatus.Approved)
        {
            return new ClientRegistrationResultDto
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                RegistrationStatus = entity.RegistrationStatus,
                Message = "Client has already been approved.",
            };
        }

        if (entity.Type == ClientType.Public)
        {
            entity.SecretHash = null;
            entity.SecretSalt = null;
            entity.SecretExpiresAt = null;
        }

        var issuedSecret = entity.Type == ClientType.Public
            ? null
            : IssueClientSecret(entity, secretExpirationDays <= 0 ? DefaultSecretExpirationDays : secretExpirationDays);

        entity.RegistrationStatus = ClientRegistrationStatus.Approved;
        entity.ReviewedTime = DateTimeOffset.UtcNow;
        entity.ReviewedBy = _userContext.UserId == Guid.Empty ? null : _userContext.UserId.ToString();
        NormalizePasswordGrantPolicy(entity);
        entity.UpdatedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLogManager.AddAuditLogAsync(
            category: "OAuth",
            eventName: "ClientRegistrationApproved",
            subjectId: entity.Id.ToString(),
            payload: JsonSerializer.Serialize(new
            {
                entity.ClientId,
                entity.DeveloperUserId,
                entity.Type,
                entity.SecretExpiresAt,
                entity.AllowPasswordGrant,
                entity.PasswordGrantRestrictionReason,
            })
        );

        return new ClientRegistrationResultDto
        {
            Id = entity.Id,
            ClientId = entity.ClientId,
            RegistrationStatus = entity.RegistrationStatus,
            Secret = issuedSecret,
            Message = entity.Type == ClientType.Public
                ? "Public client approved. No client secret was issued."
                : "Client approved and secret issued.",
        };
    }

    /// <summary>
    /// Get clients visible to the current developer portal user.
    /// </summary>
    public async Task<List<ClientDetailDto>> GetMyClientsAsync()
    {
        if (_userContext.IsAdmin)
        {
            var allIds = await _dbSet.OrderByDescending(q => q.UpdatedTime).Select(q => q.Id).ToListAsync();
            var results = new List<ClientDetailDto>();
            foreach (var clientId in allIds)
            {
                var detail = await GetDetailAsync(clientId);
                if (detail != null)
                {
                    results.Add(detail);
                }
            }

            return results;
        }

        var ids = await _dbSet
            .Where(q => q.DeveloperUserId == _userContext.UserId)
            .OrderByDescending(q => q.UpdatedTime)
            .Select(q => q.Id)
            .ToListAsync();

        var items = new List<ClientDetailDto>();
        foreach (var clientId in ids)
        {
            var detail = await GetDetailAsync(clientId);
            if (detail != null)
            {
                items.Add(detail);
            }
        }

        return items;
    }

    /// <summary>
    /// Get pending registration requests.
    /// </summary>
    public async Task<List<ClientDetailDto>> GetPendingRegistrationsAsync()
    {
        var ids = await _dbSet
            .Where(q => q.RegistrationStatus == ClientRegistrationStatus.Pending)
            .OrderBy(q => q.RequestedTime)
            .Select(q => q.Id)
            .ToListAsync();

        var items = new List<ClientDetailDto>();
        foreach (var clientId in ids)
        {
            var detail = await GetDetailAsync(clientId);
            if (detail != null)
            {
                items.Add(detail);
            }
        }

        return items;
    }

    /// <summary>
    /// Update client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <param name="dto">Client update DTO</param>
    /// <returns>Updated client detail or null</returns>
    public async Task<ClientDetailDto?> UpdateAsync(Guid id, ClientUpdateDto dto)
    {
        var entity = await _dbContext.Set<Client>()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity == null)
        {
            throw new BusinessException(Localizer.ClientNotFound, StatusCodes.Status404NotFound);
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            if (dto.DisplayName != null)
            {
                entity.DisplayName = dto.DisplayName;
            }
            if (dto.Description != null)
            {
                entity.Description = dto.Description;
            }
            if (dto.Type != null)
            {
                entity.Type = dto.Type;
            }
            if (dto.RequirePkce.HasValue)
            {
                entity.RequirePkce = dto.RequirePkce.Value;
            }
            if (dto.ConsentType != null)
            {
                entity.ConsentType = dto.ConsentType;
            }
            if (dto.ApplicationType != null)
            {
                entity.ApplicationType = dto.ApplicationType;
            }
            if (dto.AllowPasswordGrant.HasValue)
            {
                entity.AllowPasswordGrant = dto.AllowPasswordGrant.Value;
            }
            if (dto.PasswordGrantRestrictionReason != null || dto.AllowPasswordGrant == false)
            {
                entity.PasswordGrantRestrictionReason = dto.PasswordGrantRestrictionReason;
            }
            if (dto.RedirectUris != null)
            {
                entity.RedirectUris = dto.RedirectUris;
            }
            if (dto.PostLogoutRedirectUris != null)
            {
                entity.PostLogoutRedirectUris = dto.PostLogoutRedirectUris;
            }

            // Update scopes if provided - directly manipulate the join table
            if (dto.ScopeIds != null)
            {
                // Delete existing scope associations
                await _dbContext.ClientScopes
                    .Where(cs => cs.ClientId == id)
                    .ExecuteDeleteAsync();

                // Add new scope associations
                if (dto.ScopeIds.Count > 0)
                {
                    var clientScopes = dto.ScopeIds.Select(scopeId => new ClientScope
                    {
                        ClientId = id,
                        ScopeId = scopeId
                    }).ToList();

                    await _dbContext.ClientScopes.AddRangeAsync(clientScopes);
                }
            }

            // Update resources if provided - directly manipulate the join table
            if (dto.ResourceIds != null)
            {
                // Delete existing resource associations
                await _dbContext.ClientResources
                    .Where(cr => cr.ClientId == id)
                    .ExecuteDeleteAsync();

                // Add new resource associations
                if (dto.ResourceIds.Count > 0)
                {
                    var clientResources = dto.ResourceIds.Select(resourceId => new ClientResource
                    {
                        ClientId = id,
                        ApiResourceId = resourceId
                    }).ToList();

                    await _dbContext.ClientResources.AddRangeAsync(clientResources);
                }
            }

            NormalizePasswordGrantPolicy(entity);
            entity.UpdatedTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            await _auditLogManager.AddAuditLogAsync(
                category: "OAuth",
                eventName: "ClientUpdated",
                subjectId: entity.Id.ToString(),
                payload: JsonSerializer.Serialize(new
                {
                    entity.ClientId,
                    entity.AllowPasswordGrant,
                    entity.PasswordGrantRestrictionReason,
                })
            );

            return await GetDetailAsync(id);
        });
    }

    /// <summary>
    /// Delete client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>True if successful</returns>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException(Localizer.ClientNotFound, StatusCodes.Status404NotFound);
        }

        await DeleteOrUpdateAsync([id], softDelete: true);
        return true;
    }

    /// <summary>
    /// Rotate client secret
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>New secret or null if failed</returns>
    public async Task<string?> RotateSecretAsync(Guid id)
    {
        if (!await HasPermissionAsync(id))
        {
            throw new BusinessException("Forbidden", StatusCodes.Status403Forbidden);
        }

        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException(Localizer.ClientNotFound, StatusCodes.Status404NotFound);
        }

        if (entity.Type == ClientType.Public)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        foreach (var secret in await _dbContext.ClientSecrets.Where(q => q.ClientId == id && !q.RevokedAt.HasValue).ToListAsync())
        {
            secret.RevokedAt = DateTimeOffset.UtcNow;
            secret.UpdatedTime = DateTime.UtcNow;
        }

        var newSecret = IssueClientSecret(entity, DefaultSecretExpirationDays);
        entity.UpdatedTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await _auditLogManager.AddAuditLogAsync(
            category: "OAuth",
            eventName: "ClientSecretRotated",
            subjectId: entity.Id.ToString(),
            payload: JsonSerializer.Serialize(new { entity.ClientId, entity.SecretExpiresAt })
        );

        return newSecret;
    }

    /// <summary>
    /// Get client secret history.
    /// </summary>
    public async Task<List<ClientSecretHistoryDto>> GetSecretsAsync(Guid id)
    {
        if (!await HasPermissionAsync(id))
        {
            throw new BusinessException("Forbidden", StatusCodes.Status403Forbidden);
        }

        return await _dbContext.ClientSecrets
            .Where(q => q.ClientId == id)
            .OrderByDescending(q => q.CreatedTime)
            .Select(q => new ClientSecretHistoryDto
            {
                Id = q.Id,
                LastFour = q.LastFour,
                IssuedTime = q.CreatedTime,
                ExpiresAt = q.ExpiresAt,
                RevokedAt = q.RevokedAt,
                IsActive = !q.RevokedAt.HasValue && (!q.ExpiresAt.HasValue || q.ExpiresAt > DateTimeOffset.UtcNow),
            })
            .ToListAsync();
    }

    /// <summary>
    /// Assign scopes to client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <param name="scopeIds">List of scope IDs to assign</param>
    /// <returns>True if successful</returns>
    public async Task<bool> AssignScopesAsync(Guid id, List<Guid> scopeIds)
    {
        // Verify client exists
        if (!await _dbSet.AnyAsync(c => c.Id == id))
        {
            throw new BusinessException(Localizer.ClientNotFound, StatusCodes.Status404NotFound);
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            // Delete existing scope associations
            await _dbContext.ClientScopes
                .Where(cs => cs.ClientId == id)
                .ExecuteDeleteAsync();

            // Add new scope associations
            if (scopeIds.Count > 0)
            {
                var clientScopes = scopeIds.Select(scopeId => new ClientScope
                {
                    ClientId = id,
                    ScopeId = scopeId
                }).ToList();

                await _dbContext.ClientScopes.AddRangeAsync(clientScopes);
                await _dbContext.SaveChangesAsync();
            }

            return true;
        });
    }

    /// <summary>
    /// Assign resources to client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <param name="resourceIds">List of resource IDs to assign</param>
    /// <returns>True if successful</returns>
    public async Task<bool> AssignResourcesAsync(Guid id, List<Guid> resourceIds)
    {
        // Verify client exists
        if (!await _dbSet.AnyAsync(c => c.Id == id))
        {
            throw new BusinessException(Localizer.ClientNotFound, StatusCodes.Status404NotFound);
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            // Delete existing resource associations
            await _dbContext.ClientResources
                .Where(cr => cr.ClientId == id)
                .ExecuteDeleteAsync();

            // Add new resource associations
            if (resourceIds.Count > 0)
            {
                var clientResources = resourceIds.Select(resourceId => new ClientResource
                {
                    ClientId = id,
                    ApiResourceId = resourceId
                }).ToList();

                await _dbContext.ClientResources.AddRangeAsync(clientResources);
                await _dbContext.SaveChangesAsync();
            }

            return true;
        });
    }

    /// <summary>
    /// Get client authorizations
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>List of authorizations</returns>
    public async Task<List<AuthorizationItemDto>> GetAuthorizationsAsync(Guid id)
    {
        var authorizations = await _dbContext.Set<Authorization>()
            .Where(a => a.ClientId == id)
            .OrderByDescending(a => a.CreationDate)
            .Select(a => new AuthorizationItemDto
            {
                Id = a.Id,
                SubjectId = a.SubjectId,
                ClientId = a.ClientId,
                Status = a.Status,
                CreationDate = a.CreationDate
            })
            .ToListAsync();

        return authorizations;
    }

    private async Task ApplyScopesAndResourcesAsync(Client entity, List<Guid> scopeIds, List<Guid> resourceIds)
    {
        if (scopeIds.Count > 0)
        {
            var scopes = await _dbContext.ApiScopes
                .Where(s => scopeIds.Contains(s.Id))
                .ToListAsync();

            foreach (var scope in scopes)
            {
                entity.ClientScopes.Add(new ClientScope
                {
                    Client = entity,
                    Scope = scope,
                });
            }
        }

        if (resourceIds.Count > 0)
        {
            var resources = await _dbContext.ApiResources
                .Where(r => resourceIds.Contains(r.Id))
                .ToListAsync();

            foreach (var resource in resources)
            {
                entity.ClientResources.Add(new ClientResource
                {
                    Client = entity,
                    ApiResource = resource,
                });
            }
        }
    }

    private static string IssueClientSecret(Client entity, int expirationDays)
    {
        var secret = GenerateClientSecret();
        var salt = HashCrypto.BuildSalt();
        var hash = HashCrypto.GeneratePwd(secret, salt);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(expirationDays <= 0 ? DefaultSecretExpirationDays : expirationDays);

        entity.SecretSalt = salt;
        entity.SecretHash = hash;
        entity.SecretExpiresAt = expiresAt;
        entity.ClientSecrets.Add(new ClientSecret
        {
            Client = entity,
            SecretHash = hash,
            SecretSalt = salt,
            LastFour = secret.Length >= 4 ? secret[^4..] : secret,
            ExpiresAt = expiresAt,
        });

        return secret;
    }

    private static string GenerateClientSecret()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static void NormalizePasswordGrantPolicy(Client entity)
    {
        if (entity.AllowPasswordGrant)
        {
            entity.PasswordGrantRestrictionReason = null;
            return;
        }

        entity.PasswordGrantRestrictionReason = string.IsNullOrWhiteSpace(entity.PasswordGrantRestrictionReason)
            ? "Use authorization code with PKCE, device code, or client credentials instead."
            : entity.PasswordGrantRestrictionReason.Trim();
    }
}
