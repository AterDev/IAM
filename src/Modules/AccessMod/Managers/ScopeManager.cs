using AccessMod.Models.ScopeDtos;
using EntityFramework.AppDbFactory;
using Share.Exceptions;
using Microsoft.AspNetCore.Http;
using Mapster;

namespace AccessMod.Managers;

/// <summary>
/// Manager for API scope operations
/// </summary>
public class ScopeManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<ScopeManager> logger
) : ManagerBase<DefaultDbContext, ApiScope>(dbContextFactory, userContext, logger)
{
    /// <summary>
    /// Get paged scopes
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of scopes</returns>查询在
    public async Task<PageList<ScopeItemDto>> GetPageAsync(ScopeFilterDto filter)
    {
        Queryable = Queryable
            .WhereNotNull(filter.Name, q => q.Name.Contains(filter.Name!))
            .WhereNotNull(filter.DisplayName, q => q.DisplayName.Contains(filter.DisplayName!))
            .WhereNotNull(filter.Required, q => q.Required == filter.Required!.Value);

        return await PageListAsync<ScopeFilterDto, ScopeItemDto>(filter);
    }

    /// <summary>
    /// Check if user has permission to access scope
    /// </summary>
    /// <param name="id">Scope id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        // Scope management is accessible by admins for now
        // TODO: Implement proper permission checking logic
        // Security safeguard: deny by default until proper permission checks are implemented
        return await Task.FromResult(false);
    }

    /// <summary>
    /// Get scope detail by id
    /// </summary>
    /// <param name="id">Scope id</param>
    /// <returns>Scope detail or null</returns>
    public async Task<ScopeDetailDto?> GetDetailAsync(Guid id)
    {
        var scope = await Queryable
            .Include(s => s.ScopeClaims)
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();

        if (scope == null)
        {
            return null;
        }

        return new ScopeDetailDto
        {
            Id = scope.Id,
            Name = scope.Name,
            DisplayName = scope.DisplayName,
            Description = scope.Description,
            Required = scope.Required,
            Emphasize = scope.Emphasize,
            Claims = scope.ScopeClaims.Select(sc => sc.Type).ToList(),
            CreatedTime = scope.CreatedTime,
            UpdatedTime = scope.UpdatedTime
        };
    }

    /// <summary>
    /// Add new scope
    /// </summary>
    /// <param name="dto">Scope add DTO</param>
    /// <returns>Created scope detail or null</returns>
    public async Task<ScopeDetailDto?> AddAsync(ScopeAddDto dto)
    {
        if (await _dbSet.AnyAsync(q => q.Name == dto.Name))
        {
            throw new BusinessException("ScopeNameExists", StatusCodes.Status400BadRequest);
        }

        var entity = dto.MapTo<ApiScope>();

        // Add scope claims
        if (dto.Claims.Count > 0)
        {
            foreach (var claim in dto.Claims)
            {
                entity.ScopeClaims.Add(new ScopeClaim
                {
                    Scope = entity,
                    Type = claim
                });
            }
        }

        await InsertAsync(entity);
        return await GetDetailAsync(entity.Id);
    }

    /// <summary>
    /// Update scope
    /// </summary>
    /// <param name="id">Scope id</param>
    /// <param name="dto">Scope update DTO</param>
    /// <returns>Updated scope detail or null</returns>
    public async Task<ScopeDetailDto?> UpdateAsync(Guid id, ScopeUpdateDto dto)
    {
        var entity = await _dbContext.Set<ApiScope>()
            .Include(s => s.ScopeClaims)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (entity == null)
        {
            throw new BusinessException("ScopeNotFound", StatusCodes.Status404NotFound);
        }

        if (dto.DisplayName != null)
        {
            entity.DisplayName = dto.DisplayName;
        }
        if (dto.Description != null)
        {
            entity.Description = dto.Description;
        }
        if (dto.Required.HasValue)
        {
            entity.Required = dto.Required.Value;
        }
        if (dto.Emphasize.HasValue)
        {
            entity.Emphasize = dto.Emphasize.Value;
        }

        // Update claims if provided
        if (dto.Claims != null)
        {
            entity.ScopeClaims.Clear();
            foreach (var claim in dto.Claims)
            {
                entity.ScopeClaims.Add(new ScopeClaim
                {
                    Scope = entity,
                    Type = claim
                });
            }
        }

        entity.UpdatedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return await GetDetailAsync(id);
    }

    /// <summary>
    /// Delete scope
    /// </summary>
    /// <param name="id">Scope id</param>
    /// <returns>True if successful</returns>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException("ScopeNotFound", StatusCodes.Status404NotFound);
        }

        await DeleteOrUpdateAsync([id], softDelete: true);
        return true;
    }
}
