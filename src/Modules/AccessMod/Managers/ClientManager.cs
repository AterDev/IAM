using System.Security.Cryptography;
using AccessMod.Models.AuthorizationDtos;
using AccessMod.Models.ClientDtos;
using AccessMod.Models.ResourceDtos;
using AccessMod.Models.ScopeDtos;
using Share.Services;

namespace AccessMod.Managers;

/// <summary>
/// Manager for OAuth/OIDC client operations
/// </summary>
public class ClientManager(
    DefaultDbContext dbContext,
    IPasswordHasher passwordHasher,
    ILogger<ClientManager> logger)
    : ManagerBase<DefaultDbContext, Client>(dbContext, logger)
{
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

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

        return await ToPageAsync<ClientFilterDto, ClientItemDto>(filter);
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
            CreatedTime = client.CreatedTime,
            UpdatedTime = client.UpdatedTime
        };
    }

    /// <summary>
    /// Add new client
    /// </summary>
    /// <param name="dto">Client add DTO</param>
    /// <returns>Created client detail with secret or null</returns>
    public async Task<(ClientDetailDto? Detail, string? Secret)> AddAsync(ClientAddDto dto)
    {
        if (await ExistAsync(q => q.ClientId == dto.ClientId))
        {
            ErrorMsg = "Client ID already exists";
            return (null, null);
        }

        // Generate client secret
        var secret = GenerateClientSecret();
        var hashedSecret = _passwordHasher.HashPassword(secret);

        var entity = new Client
        {
            ClientId = dto.ClientId,
            ClientSecret = hashedSecret,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            Type = dto.Type,
            RequirePkce = dto.RequirePkce,
            ConsentType = dto.ConsentType,
            ApplicationType = dto.ApplicationType,
            RedirectUris = dto.RedirectUris,
            PostLogoutRedirectUris = dto.PostLogoutRedirectUris
        };

        // Start transaction for consistency
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // Add client scopes
            if (dto.ScopeIds.Count > 0)
            {
                var scopes = await _dbContext.ApiScopes
                    .Where(s => dto.ScopeIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var scope in scopes)
                {
                    entity.ClientScopes.Add(new ClientScope
                    {
                        Client = entity,
                        Scope = scope
                    });
                }
            }

            // Add client resources
            if (dto.ResourceIds.Count > 0)
            {
                var resources = await _dbContext.ApiResources
                    .Where(r => dto.ResourceIds.Contains(r.Id))
                    .ToListAsync();

                foreach (var resource in resources)
                {
                    entity.ClientResources.Add(new ClientResource
                    {
                        Client = entity,
                        ApiResource = resource
                    });
                }
            }

            var success = await AddAsync(entity);
            if (!success)
            {
                await transaction.RollbackAsync();
                return (null, null);
            }

            await transaction.CommitAsync();
            var detail = await GetDetailAsync(entity.Id);
            return (detail, secret);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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
            ErrorMsg = "Client not found";
            return null;
        }

        // Start transaction for consistency
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
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

            var success = await SaveChangesAsync() > 0;
            if (success)
            {
                await transaction.CommitAsync();
                return await GetDetailAsync(id);
            }

            await transaction.RollbackAsync();
            return null;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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
            ErrorMsg = "Client not found";
            return false;
        }

        return await DeleteAsync(entity);
    }

    /// <summary>
    /// Rotate client secret
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>New secret or null if failed</returns>
    public async Task<string?> RotateSecretAsync(Guid id)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            ErrorMsg = "Client not found";
            return null;
        }

        var newSecret = GenerateClientSecret();
        entity.ClientSecret = _passwordHasher.HashPassword(newSecret);

        var success = await UpdateAsync(entity);
        return !success ? null : newSecret;
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
        if (!await ExistAsync(c => c.Id == id))
        {
            ErrorMsg = "Client not found";
            return false;
        }

        // Start transaction for consistency
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
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
                var success = await SaveChangesAsync() > 0;
                if (success)
                {
                    await transaction.CommitAsync();
                    return true;
                }
            }
            else
            {
                // No new scopes to add, just return success
                await transaction.CommitAsync();
                return true;
            }

            await transaction.RollbackAsync();
            return false;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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
        if (!await ExistAsync(c => c.Id == id))
        {
            ErrorMsg = "Client not found";
            return false;
        }

        // Start transaction for consistency
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
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
                var success = await SaveChangesAsync() > 0;
                if (success)
                {
                    await transaction.CommitAsync();
                    return true;
                }
            }
            else
            {
                // No new resources to add, just return success
                await transaction.CommitAsync();
                return true;
            }

            await transaction.RollbackAsync();
            return false;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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

    /// <summary>
    /// Generate a cryptographically secure client secret
    /// </summary>
    /// <returns>Base64-encoded random string</returns>
    private static string GenerateClientSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
