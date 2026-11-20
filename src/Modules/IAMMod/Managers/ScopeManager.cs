using Perigon.PostgreSQL;
using EntityFramework.AppDbFactory;
using Share.Exceptions;
using Microsoft.AspNetCore.Http;
using Entity.IAMMod;
using IAMMod.Models.ScopeDtos;

namespace IAMMod.Managers;

/// <summary>
/// Manager for API scope operations
/// </summary>
public class ScopeManager(
    AppDbFactory dbContextFactory,
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
        var query = _dbSet
            .WhereNotNull(filter.Name, q => q.Name.Contains(filter.Name!))
            .WhereNotNull(filter.DisplayName, q => q.DisplayName.Contains(filter.DisplayName!))
            .WhereNotNull(filter.Required, q => q.Required == filter.Required);

        var count = await query.CountAsync();
        var rows = await query.OrderByDescending(item => item.CreatedTime).Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize).ToListAsync();
        return new PageList<ScopeItemDto> { Count = count, PageIndex = filter.PageIndex, Data = rows.Select(ScopeItemDto.FromEntity).ToList() };
    }

    /// <summary>
    /// Check if user has permission to access scope
    /// </summary>
    /// <param name="id">Scope id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        return await Task.FromResult(_userContext.IsAdmin);
    }

    /// <summary>
    /// Get scope detail by id
    /// </summary>
    /// <param name="id">Scope id</param>
    /// <returns>Scope detail or null</returns>
    public async Task<ScopeDetailDto?> GetDetailAsync(Guid id)
    {
        var scope = await Queryable

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
            Claims = await _dbContext.ScopeClaims.Where(item => item.ScopeId == id).Select(item => item.Type).ToScalarListAsync(),
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
        if (await _dbSet.Where(q => q.Name == dto.Name).AnyAsync())
        {
            throw new BusinessException("ScopeNameExists", StatusCodes.Status400BadRequest);
        }

        var entity = new ApiScope
        {
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            Required = dto.Required,
            Emphasize = dto.Emphasize,
        };
        await _dbContext.TransactionAsync(async ct =>
        {
            await InsertAsync(entity, ct);
            foreach (var claim in dto.Claims)
            {
                await _dbContext.ScopeClaims.InsertAsync(new ScopeClaim { ScopeId = entity.Id, Type = claim }, ct);
            }
        });
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

            .Where(s => s.Id == id).FirstOrDefaultAsync();

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

        var updatedAt = DateTimeOffset.UtcNow;
        await _dbContext.TransactionAsync(async ct =>
        {
            await _dbSet.Where(item => item.Id == id).ExecuteUpdateAsync(update => update
                .Set(item => item.DisplayName, entity.DisplayName)
                .Set(item => item.Description, entity.Description)
                .Set(item => item.Required, entity.Required)
                .Set(item => item.Emphasize, entity.Emphasize)
                .Set(item => item.UpdatedTime, updatedAt), cancellationToken: ct);
            if (dto.Claims is not null)
            {
                await _dbContext.ScopeClaims.Where(item => item.ScopeId == id).ExecuteDeleteAsync(cancellationToken: ct);
                foreach (var claim in dto.Claims)
                {
                    await _dbContext.ScopeClaims.InsertAsync(new ScopeClaim { ScopeId = id, Type = claim }, ct);
                }
            }
        });
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
