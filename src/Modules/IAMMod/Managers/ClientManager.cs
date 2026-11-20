using IAMMod.Models.AuthorizationDtos;
using IAMMod.Models.ClientDtos;
using IAMMod.Models.ResourceDtos;
using IAMMod.Models.ScopeDtos;
using IAMMod.Serialization;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using Share;
using Share.Exceptions;

namespace IAMMod.Managers;

/// <summary>
/// Manager for OAuth/OIDC client operations
/// </summary>
public class ClientManager(
    AppDbFactory dbContextFactory,
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

        var ordered = Queryable.OrderByDescending(item => item.CreatedTime);
        var count = await ordered.CountAsync();
        var clients = await ordered
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();
        ResetQuery();
        return new PageList<ClientItemDto>
        {
            Count = count,
            PageIndex = filter.PageIndex,
            Data = clients.Select(item => new ClientItemDto
            {
                Id = item.Id,
                ClientId = item.ClientIdentifier,
                DisplayName = item.DisplayName,
                Type = item.Type,
                ApplicationType = item.ApplicationType,
                CreatedTime = item.CreatedTime,
            }).ToList(),
        };
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

        return await _dbSet
            .Where(q => q.Id == id && q.DeveloperUserId == _userContext.UserId)
            .AnyAsync();
    }

    /// <summary>
    /// Get client detail by id
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>Client detail or null</returns>
    public async Task<ClientDetailDto?> GetDetailAsync(Guid id)
    {
        var client = await Queryable.Where(c => c.Id == id).FirstOrDefaultAsync();

        if (client == null)
        {
            return null;
        }

        var clientScopes = await _dbContext.ClientScopes.Where(item => item.ClientId == id).ToListAsync();
        var scopeIds = clientScopes.Select(item => item.ScopeId).ToArray();
        var scopes = scopeIds.Length == 0
            ? []
            : await _dbContext.ApiScopes.Where(item => Enumerable.Contains(scopeIds, item.Id)).ToListAsync();
        var clientResources = await _dbContext.ClientResources.Where(item => item.ClientId == id).ToListAsync();
        var resourceIds = clientResources.Select(item => item.ApiResourceId).ToArray();
        var resources = resourceIds.Length == 0
            ? []
            : await _dbContext.ApiResources.Where(item => Enumerable.Contains(resourceIds, item.Id)).ToListAsync();
        var secrets = await _dbContext.ClientSecrets
            .Where(item => item.ClientId == id)
            .OrderByDescending(item => item.CreatedTime)
            .ToListAsync();

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
            RedirectUris = client.RedirectUris,
            PostLogoutRedirectUris = client.PostLogoutRedirectUris,
            Scopes = scopes.Select(scope => new ScopeItemDto
            {
                Id = scope.Id,
                Name = scope.Name,
                DisplayName = scope.DisplayName,
                Required = scope.Required,
                CreatedTime = scope.CreatedTime
            }).ToList(),
            Resources = resources.Select(resource => new ClientResourceDto
            {
                Id = resource.Id,
                Name = resource.Name,
                DisplayName = resource.DisplayName,
                Description = resource.Description,
                CreatedTime = resource.CreatedTime
            }).ToList(),
            Secrets = secrets
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
        if (await _dbSet.Where(q => q.ClientIdentifier == dto.ClientId).AnyAsync())
        {
            throw new BusinessException(Localizer.EntityNotFound, StatusCodes.Status400BadRequest);
        }

        var entity = new Client
        {
            ClientIdentifier = dto.ClientId,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            Type = dto.Type,
            RequirePkce = dto.RequirePkce,
            ConsentType = dto.ConsentType,
            ApplicationType = dto.ApplicationType,
            RedirectUris = dto.RedirectUris,
            PostLogoutRedirectUris = dto.PostLogoutRedirectUris,
        };
        entity.RegistrationStatus = ClientRegistrationStatus.Approved;
        var issuedSecret = IssueClientSecret(entity, DefaultSecretExpirationDays);

        return await ExecuteInTransactionAsync(async () =>
        {
            await ApplyScopesAndResourcesAsync(entity, dto.ScopeIds, dto.ResourceIds);
            await InsertAsync(entity);
            if (entity.ClientScopes.Count > 0)
            {
                await _dbContext.ClientScopes.BulkInsertAsync(entity.ClientScopes);
            }
            if (entity.ClientResources.Count > 0)
            {
                await _dbContext.ClientResources.BulkInsertAsync(entity.ClientResources);
            }

            await _auditLogManager.AddAuditLogAsync(
                category: "OAuth",
                eventName: "ClientCreated",
                subjectId: entity.Id.ToString(),
                payload: IamJson.Serialize(new IamAuditPayload
                {
                    ClientIdentifier = entity.ClientIdentifier,
                    DisplayName = entity.DisplayName,
                    RegistrationStatus = entity.RegistrationStatus,
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

        if (await _dbSet.Where(q => q.ClientIdentifier == dto.ClientId).AnyAsync())
        {
            throw new BusinessException(Localizer.EntityNotFound, StatusCodes.Status400BadRequest);
        }

        var entity = new Client
        {
            ClientIdentifier = dto.ClientId,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            Type = dto.Type,
            RequirePkce = dto.RequirePkce,
            ConsentType = dto.ConsentType,
            ApplicationType = dto.ApplicationType,
            RedirectUris = dto.RedirectUris,
            PostLogoutRedirectUris = dto.PostLogoutRedirectUris,
        };
        entity.RegistrationStatus = ClientRegistrationStatus.Pending;
        entity.DeveloperUserId = _userContext.UserId;
        entity.RequestedTime = DateTimeOffset.UtcNow;

        return await ExecuteInTransactionAsync(async () =>
        {
            await ApplyScopesAndResourcesAsync(entity, dto.ScopeIds, dto.ResourceIds);
            await InsertAsync(entity);
            if (entity.ClientScopes.Count > 0)
            {
                await _dbContext.ClientScopes.BulkInsertAsync(entity.ClientScopes);
            }
            if (entity.ClientResources.Count > 0)
            {
                await _dbContext.ClientResources.BulkInsertAsync(entity.ClientResources);
            }

            await _auditLogManager.AddAuditLogAsync(
                category: "OAuth",
                eventName: "ClientRegistrationRequested",
                subjectId: entity.Id.ToString(),
                payload: IamJson.Serialize(new IamAuditPayload
                {
                    ClientIdentifier = entity.ClientIdentifier,
                    DisplayName = entity.DisplayName,
                    DeveloperUserId = entity.DeveloperUserId,
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
        entity.UpdatedTime = DateTime.UtcNow;
        await _dbSet
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.SecretHash, entity.SecretHash)
                .Set(item => item.SecretSalt, entity.SecretSalt)
                .Set(item => item.SecretExpiresAt, entity.SecretExpiresAt)
                .Set(item => item.RegistrationStatus, entity.RegistrationStatus)
                .Set(item => item.ReviewedTime, entity.ReviewedTime)
                .Set(item => item.ReviewedBy, entity.ReviewedBy)
                .Set(item => item.UpdatedTime, entity.UpdatedTime));

        await _auditLogManager.AddAuditLogAsync(
            category: "OAuth",
            eventName: "ClientRegistrationApproved",
            subjectId: entity.Id.ToString(),
            payload: IamJson.Serialize(new IamAuditPayload
            {
                ClientId = entity.ClientId,
                DeveloperUserId = entity.DeveloperUserId,
                Type = entity.Type,
                SecretExpiresAt = entity.SecretExpiresAt,
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
            var allIds = await _dbSet.OrderByDescending(q => q.UpdatedTime).Select(q => q.Id).ToScalarListAsync();
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
            .ToScalarListAsync();

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
            .ToScalarListAsync();

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
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();

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

                    await _dbContext.ClientScopes.BulkInsertAsync(clientScopes);
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

                    await _dbContext.ClientResources.BulkInsertAsync(clientResources);
                }
            }

            if (entity.Type == ClientType.Public)
            {
                entity.SecretHash = null;
                entity.SecretSalt = null;
                entity.SecretExpiresAt = null;

                await _dbContext.ClientSecrets
                    .Where(q => q.ClientId == id && !q.RevokedAt.HasValue)
                    .ExecuteUpdateAsync(update => update
                        .Set(item => item.RevokedAt, DateTimeOffset.UtcNow)
                        .Set(item => item.UpdatedTime, DateTimeOffset.UtcNow));
            }

            entity.UpdatedTime = DateTime.UtcNow;
            await _dbSet
                .Where(item => item.Id == id)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.DisplayName, entity.DisplayName)
                    .Set(item => item.Description, entity.Description)
                    .Set(item => item.Type, entity.Type)
                    .Set(item => item.RequirePkce, entity.RequirePkce)
                    .Set(item => item.ConsentType, entity.ConsentType)
                    .Set(item => item.ApplicationType, entity.ApplicationType)
                    .Set(item => item.RedirectUriValues, entity.RedirectUriValues)
                    .Set(item => item.PostLogoutRedirectUriValues, entity.PostLogoutRedirectUriValues)
                    .Set(item => item.SecretHash, entity.SecretHash)
                    .Set(item => item.SecretSalt, entity.SecretSalt)
                    .Set(item => item.SecretExpiresAt, entity.SecretExpiresAt)
                    .Set(item => item.UpdatedTime, entity.UpdatedTime));

            await _auditLogManager.AddAuditLogAsync(
                category: "OAuth",
                eventName: "ClientUpdated",
                subjectId: entity.Id.ToString(),
                payload: IamJson.Serialize(new IamAuditPayload
                {
                    ClientId = entity.ClientId,
                    Type = entity.Type,
                    ApplicationType = entity.ApplicationType,
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

        await _dbContext.ClientSecrets
            .Where(q => q.ClientId == id && !q.RevokedAt.HasValue)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.RevokedAt, DateTimeOffset.UtcNow)
                .Set(item => item.UpdatedTime, DateTimeOffset.UtcNow));

        var newSecret = IssueClientSecret(entity, DefaultSecretExpirationDays);
        entity.UpdatedTime = DateTime.UtcNow;
        await _dbSet
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.SecretHash, entity.SecretHash)
                .Set(item => item.SecretSalt, entity.SecretSalt)
                .Set(item => item.SecretExpiresAt, entity.SecretExpiresAt)
                .Set(item => item.UpdatedTime, entity.UpdatedTime));

        await _auditLogManager.AddAuditLogAsync(
            category: "OAuth",
            eventName: "ClientSecretRotated",
            subjectId: entity.Id.ToString(),
            payload: IamJson.Serialize(new IamAuditPayload { ClientId = entity.ClientId, SecretExpiresAt = entity.SecretExpiresAt })
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

        var secrets = await _dbContext.ClientSecrets
            .Where(q => q.ClientId == id)
            .OrderByDescending(q => q.CreatedTime).ToListAsync();
        return secrets.Select(q => new ClientSecretHistoryDto
            {
                Id = q.Id,
                LastFour = q.LastFour,
                IssuedTime = q.CreatedTime,
                ExpiresAt = q.ExpiresAt,
                RevokedAt = q.RevokedAt,
                IsActive = !q.RevokedAt.HasValue && (!q.ExpiresAt.HasValue || q.ExpiresAt > DateTimeOffset.UtcNow),
            }).ToList();
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
        if (!await _dbSet.Where(c => c.Id == id).AnyAsync())
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

                await _dbContext.ClientScopes.BulkInsertAsync(clientScopes);
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
        if (!await _dbSet.Where(c => c.Id == id).AnyAsync())
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

                await _dbContext.ClientResources.BulkInsertAsync(clientResources);
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
            .OrderByDescending(a => a.CreationDate).ToListAsync();
        return authorizations.Select(a => new AuthorizationItemDto
            {
                Id = a.Id,
                SubjectId = a.SubjectId,
                ClientId = a.ClientId,
                Status = a.Status,
                CreationDate = a.CreationDate
            }).ToList();
    }

    private async Task ApplyScopesAndResourcesAsync(Client entity, List<Guid> scopeIds, List<Guid> resourceIds)
    {
        entity.ClientScopes = scopeIds.Distinct().Select(scopeId => new ClientScope
        {
            ClientId = entity.Id,
            ScopeId = scopeId,
        }).ToList();
        entity.ClientResources = resourceIds.Distinct().Select(resourceId => new ClientResource
        {
            ClientId = entity.Id,
            ApiResourceId = resourceId,
        }).ToList();
        await Task.CompletedTask;
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
}
