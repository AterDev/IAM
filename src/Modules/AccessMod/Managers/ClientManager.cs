using AccessMod.Models.ClientDtos;
using Entity.Access;
using EntityFramework.DBProvider;
using Microsoft.EntityFrameworkCore;
using Share.Services;
using System.Security.Cryptography;

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
            .WhereNotNull(filter.ClientId != null, q => q.ClientId.Contains(filter.ClientId!))
            .WhereNotNull(filter.DisplayName != null, q => q.DisplayName.Contains(filter.DisplayName!))
            .WhereNotNull(filter.Type != null, q => q.Type == filter.Type)
            .WhereNotNull(filter.ApplicationType != null, q => q.ApplicationType == filter.ApplicationType);

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
            Scopes = client.ClientScopes.Select(cs => cs.Scope.Name).ToList(),
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

        // Add client scopes
        if (dto.ScopeIds.Count > 0)
        {
            var scopes = await _context.Set<ApiScope>()
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

        var success = await AddAsync(entity);
        if (!success)
        {
            return (null, null);
        }

        var detail = await GetDetailAsync(entity.Id);
        return (detail, secret);
    }

    /// <summary>
    /// Update client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <param name="dto">Client update DTO</param>
    /// <returns>Updated client detail or null</returns>
    public async Task<ClientDetailDto?> UpdateAsync(Guid id, ClientUpdateDto dto)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            ErrorMsg = "Client not found";
            return null;
        }

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

        var success = await UpdateAsync(entity);
        return !success ? null : await GetDetailAsync(id);
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
        var entity = await _context.Set<Client>()
            .Include(c => c.ClientScopes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity == null)
        {
            ErrorMsg = "Client not found";
            return false;
        }

        // Remove existing scopes
        entity.ClientScopes.Clear();

        // Add new scopes
        var scopes = await _context.Set<ApiScope>()
            .Where(s => scopeIds.Contains(s.Id))
            .ToListAsync();

        foreach (var scope in scopes)
        {
            entity.ClientScopes.Add(new ClientScope
            {
                Client = entity,
                Scope = scope
            });
        }

        return await SaveAsync() > 0;
    }

    /// <summary>
    /// Get client authorizations
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>List of authorizations</returns>
    public async Task<List<AuthorizationItemDto>> GetAuthorizationsAsync(Guid id)
    {
        var authorizations = await _context.Set<Authorization>()
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
