using Perigon.PostgreSQL;
using EntityFramework.AppDbContext;
using EntityFramework.AppDbFactory;
using Share.Exceptions;
using Microsoft.AspNetCore.Http;
using Entity.IAMMod;
using IAMMod.Models.ResourceDtos;

namespace IAMMod.Managers;

/// <summary>
/// Manager for API resource operations
/// </summary>
public class ResourceManager(
    AppDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<ResourceManager> logger
) : ManagerBase<DefaultDbContext, ApiResource>(dbContextFactory, userContext, logger)
{
    /// <summary>
    /// Get paged resources
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of resources</returns>
    public async Task<PageList<ResourceItemDto>> GetPageAsync(ResourceFilterDto filter)
    {
        var query = _dbSet
            .WhereNotNull(filter.Name, q => q.Name.Contains(filter.Name!))
            .WhereNotNull(filter.DisplayName, q => q.DisplayName.Contains(filter.DisplayName!));

        var count = await query.CountAsync();
        var rows = await query.OrderByDescending(item => item.CreatedTime).Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize).ToListAsync();
        return new PageList<ResourceItemDto> { Count = count, PageIndex = filter.PageIndex, Data = rows.Select(ResourceItemDto.FromEntity).ToList() };
    }

    /// <summary>
    /// Check if user has permission to access resource
    /// </summary>
    /// <param name="id">Resource id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        return await Task.FromResult(_userContext.IsAdmin);
    }

    /// <summary>
    /// Get resource detail by id
    /// </summary>
    /// <param name="id">Resource id</param>
    /// <returns>Resource detail or null</returns>
    public async Task<ResourceDetailDto?> GetDetailAsync(Guid id)
    {
        var entity = await FindAsync(id);
        return entity is null ? null : ResourceDetailDto.FromEntity(entity);
    }

    /// <summary>
    /// Add new resource
    /// </summary>
    /// <param name="dto">Resource add DTO</param>
    /// <returns>Created resource detail or null</returns>
    public async Task<ResourceDetailDto?> AddAsync(ResourceAddDto dto)
    {
        if (await _dbSet.Where(q => q.Name == dto.Name).AnyAsync())
        {
            throw new BusinessException("ResourceNameExists", StatusCodes.Status400BadRequest);
        }

        var entity = new ApiResource { Name = dto.Name, DisplayName = dto.DisplayName, Description = dto.Description };
        await InsertAsync(entity);
        return await GetDetailAsync(entity.Id);
    }

    /// <summary>
    /// Update resource
    /// </summary>
    /// <param name="id">Resource id</param>
    /// <param name="dto">Resource update DTO</param>
    /// <returns>Updated resource detail or null</returns>
    public async Task<ResourceDetailDto?> UpdateAsync(Guid id, ResourceUpdateDto dto)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException("ResourceNotFound", StatusCodes.Status404NotFound);
        }

        if (dto.DisplayName != null)
        {
            entity.DisplayName = dto.DisplayName;
        }
        if (dto.Description != null)
        {
            entity.Description = dto.Description;
        }

        entity.UpdatedTime = DateTime.UtcNow;
        await _dbSet.Where(item => item.Id == id).ExecuteUpdateAsync(update => update
            .Set(item => item.DisplayName, entity.DisplayName)
            .Set(item => item.Description, entity.Description)
            .Set(item => item.UpdatedTime, entity.UpdatedTime));
        return await GetDetailAsync(id);
    }

    /// <summary>
    /// Delete resource
    /// </summary>
    /// <param name="id">Resource id</param>
    /// <returns>True if successful</returns>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException("ResourceNotFound", StatusCodes.Status404NotFound);
        }

        await DeleteOrUpdateAsync([id], softDelete: true);
        return true;
    }

    /// <summary>
    /// Get all resources (for dropdown/selection)
    /// </summary>
    /// <returns>List of all resources</returns>
    public async Task<List<ResourceItemDto>> GetAllAsync()
    {
        var rows = await _dbSet.ToListAsync();
        return rows.Select(ResourceItemDto.FromEntity).ToList();
    }
}
