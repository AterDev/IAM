using System.Security.Cryptography;
using AccessMod.Models.AuthorizationDtos;
using AccessMod.Models.ClientDtos;
using AccessMod.Models.ResourceDtos;
using AccessMod.Models.ScopeDtos;
using Share.Services;
using EntityFramework.AppDbFactory;
using Share.Exceptions;
using Microsoft.AspNetCore.Http;
using Mapster;

namespace AccessMod.Managers;

/// <summary>
/// Manager for OAuth/OIDC client operations
/// </summary>
public class ClientManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<ClientManager> logger,
    IPasswordHasher passwordHasher
) : ManagerBase<DefaultDbContext, Client>(dbContextFactory, userContext, logger)
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

        return await PageListAsync<ClientFilterDto, ClientItemDto>(filter);
    }

    /// <summary>
    /// Check if user has permission to access client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        // Client management is accessible by admins for now
        return await Task.FromResult(true);
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
        if (await _dbSet.AnyAsync(q => q.ClientId == dto.ClientId))
        {
            throw new BusinessException("ClientIdExists", StatusCodes.Status400BadRequest);
        }

        // Generate client secret
        var secret = GenerateClientSecret();
        var hashedSecret = _passwordHasher.HashPassword(secret);

        var entity = dto.MapTo<Client>();
        entity.ClientSecret = hashedSecret;

        return await ExecuteInTransactionAsync(async () =>
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

            await InsertAsync(entity);
            var detail = await GetDetailAsync(entity.Id);
            return (detail, secret);
        });
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
            throw new BusinessException("ClientNotFound", StatusCodes.Status404NotFound);
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

            entity.UpdatedTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
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
            throw new BusinessException("ClientNotFound", StatusCodes.Status404NotFound);
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
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException("ClientNotFound", StatusCodes.Status404NotFound);
        }

        var newSecret = GenerateClientSecret();
        entity.ClientSecret = _passwordHasher.HashPassword(newSecret);
        entity.UpdatedTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return newSecret;
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
            throw new BusinessException("ClientNotFound", StatusCodes.Status404NotFound);
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
            throw new BusinessException("ClientNotFound", StatusCodes.Status404NotFound);
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
