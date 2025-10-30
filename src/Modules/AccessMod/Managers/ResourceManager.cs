using AccessMod.Models.ResourceDtos;
using Entity.AccessMod;
using EntityFramework.DBProvider;

namespace AccessMod.Managers;

/// <summary>
/// Manager for API resource operations
/// </summary>
public class ResourceManager(DefaultDbContext dbContext, ILogger<ResourceManager> logger)
    : ManagerBase<DefaultDbContext, ApiResource>(dbContext, logger)
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

        return await ToPageAsync<ResourceFilterDto, ResourceItemDto>(filter);
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
        if (await ExistAsync(q => q.Name == dto.Name))
        {
            ErrorMsg = "Resource name already exists";
            return null;
        }

        var entity = new ApiResource
        {
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Description = dto.Description
        };

        var success = await AddAsync(entity);
        return !success ? null : await GetDetailAsync(entity.Id);
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
            ErrorMsg = "Resource not found";
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

        var success = await UpdateAsync(entity);
        return !success ? null : await GetDetailAsync(id);
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
            ErrorMsg = "Resource not found";
            return false;
        }

        return await DeleteAsync(entity);
    }
}
