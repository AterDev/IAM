using EntityFramework.AppDbContext;
using EntityFramework.AppDbFactory;
using Share.Exceptions;
using Microsoft.AspNetCore.Http;
using Mapster;
using Entity.IAMMod;
using IAMMod.Models.ResourceDtos;

namespace IAMMod.Managers;

/// <summary>
/// Manager for API resource operations
/// </summary>
public class ResourceManager(
    TenantDbFactory dbContextFactory,
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
        Queryable = Queryable
            .WhereNotNull(filter.Name, q => q.Name.Contains(filter.Name!))
            .WhereNotNull(filter.DisplayName, q => q.DisplayName.Contains(filter.DisplayName!));

        return await PageListAsync<ResourceFilterDto, ResourceItemDto>(filter);
    }

    /// <summary>
    /// Check if user has permission to access resource
    /// </summary>
    /// <param name="id">Resource id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        // Resource management is accessible by admins for now
        // TODO: Implement proper permission checking logic
        // Security safeguard: deny by default until proper permission checks are implemented
        return await Task.FromResult(false);
    }

    /// <summary>
    /// Get resource detail by id
    /// </summary>
    /// <param name="id">Resource id</param>
    /// <returns>Resource detail or null</returns>
    public async Task<ResourceDetailDto?> GetDetailAsync(Guid id)
    {
        return await FindAsync<ResourceDetailDto>(q => q.Id == id);
    }

    /// <summary>
    /// Add new resource
    /// </summary>
    /// <param name="dto">Resource add DTO</param>
    /// <returns>Created resource detail or null</returns>
    public async Task<ResourceDetailDto?> AddAsync(ResourceAddDto dto)
    {
        if (await _dbSet.AnyAsync(q => q.Name == dto.Name))
        {
            throw new BusinessException("ResourceNameExists", StatusCodes.Status400BadRequest);
        }

        var entity = dto.MapTo<ApiResource>();
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
        await _dbContext.SaveChangesAsync();
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
        return await Queryable
            .Select(r => new ResourceItemDto
            {
                Id = r.Id,
                Name = r.Name,
                DisplayName = r.DisplayName,
                Description = r.Description,
                CreatedTime = r.CreatedTime
            })
            .ToListAsync();
    }
}
