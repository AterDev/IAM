using IAMMod.Models.RoleDtos;
using Microsoft.AspNetCore.Http;
using Share.Exceptions;

namespace IAMMod.Managers;

/// <summary>
/// Manager for role operations
/// </summary>
public class RoleManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<RoleManager> logger)
    : ManagerBase<DefaultDbContext, Role>(dbContextFactory, userContext, logger)
{
    /// <summary>
    /// Get paged roles
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of roles</returns>
    public async Task<PageList<RoleItemDto>> GetPageAsync(RoleFilterDto filter)
    {
        Queryable = Queryable
            .WhereNotNull(filter.Name, q => q.Name.Contains(filter.Name!))
            .WhereNotNull(filter.StartDate, q => q.CreatedTime >= filter.StartDate)
            .WhereNotNull(filter.EndDate, q => q.CreatedTime <= filter.EndDate);

        return await PageListAsync<RoleFilterDto, RoleItemDto>(filter);
    }

    /// <summary>
    /// Check if user has permission to access role
    /// </summary>
    /// <param name="id">Role id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        return await Task.FromResult(_userContext.IsAdmin);
    }

    /// <summary>
    /// Get role detail by id
    /// </summary>
    /// <param name="id">Role id</param>
    /// <returns>Role detail or null</returns>
    public async Task<RoleDetailDto?> GetDetailAsync(Guid id)
    {
        return await FindAsync<RoleDetailDto>(q => q.Id == id);
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    /// <param name="name">Role name</param>
    /// <returns>Role detail or null</returns>
    public async Task<RoleDetailDto?> GetByNameAsync(string name)
    {
        var normalizedName = name.ToUpperInvariant();
        return await FindAsync<RoleDetailDto>(q => q.NormalizedName == normalizedName);
    }

    /// <summary>
    /// Add new role
    /// </summary>
    /// <param name="dto">Role add DTO</param>
    /// <returns>Created role detail or null</returns>
    public async Task<RoleDetailDto?> AddAsync(RoleAddDto dto)
    {
        var normalizedName = dto.Name.ToUpperInvariant();

        // Check if role name already exists
        if (await _dbSet.AnyAsync(q => q.NormalizedName == normalizedName))
        {
            throw new BusinessException("RoleNameExists", StatusCodes.Status400BadRequest);
        }

        var entity = dto.MapTo<Role>();
        entity.NormalizedName = normalizedName;
        entity.ConcurrencyStamp = Guid.NewGuid().ToString();

        await InsertAsync(entity);
        return await GetDetailAsync(entity.Id);
    }

    /// <summary>
    /// Update role
    /// </summary>
    /// <param name="id">Role id</param>
    /// <param name="dto">Role update DTO</param>
    /// <returns>Updated role detail or null</returns>
    public async Task<RoleDetailDto?> UpdateAsync(Guid id, RoleUpdateDto dto)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException("RoleNotFound", StatusCodes.Status404NotFound);
        }

        // Check if name already exists (if changing)
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != entity.Name)
        {
            var normalizedName = dto.Name.ToUpperInvariant();
            if (await _dbSet.AnyAsync(q => q.NormalizedName == normalizedName && q.Id != id))
            {
                throw new BusinessException("RoleNameExists", StatusCodes.Status400BadRequest);
            }
            entity.Name = dto.Name;
            entity.NormalizedName = normalizedName;
        }

        if (dto.Description != null)
        {
            entity.Description = dto.Description;
        }

        entity.ConcurrencyStamp = Guid.NewGuid().ToString();
        entity.UpdatedTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return await GetDetailAsync(id);
    }

    /// <summary>
    /// Delete role (soft delete)
    /// </summary>
    /// <param name="id">Role id</param>
    /// <param name="softDelete">Perform soft delete (default true)</param>
    /// <returns>True if successful</returns>
    public async Task<bool> DeleteAsync(Guid id, bool softDelete = true)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException("RoleNotFound", StatusCodes.Status404NotFound);
        }

        // Check if role has users
        await LoadManyAsync(entity, r => r.UserRoles);
        if (entity.UserRoles.Count > 0)
        {
            throw new BusinessException("RoleHasUsers", StatusCodes.Status400BadRequest);
        }

        await DeleteOrUpdateAsync([id], softDelete);
        return true;
    }

    /// <summary>
    /// Get all roles (for dropdowns/selects)
    /// </summary>
    /// <returns>List of all roles</returns>
    public async Task<List<RoleItemDto>> GetAllAsync()
    {
        return await ListAsync<RoleItemDto>();
    }

    /// <summary>
    /// Get role names by IDs
    /// </summary>
    /// <param name="roleIds">List of role IDs</param>
    /// <returns>List of role names</returns>
    public async Task<List<string>> GetRoleNamesByIdsAsync(List<Guid> roleIds)
    {
        return await Queryable
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync();
    }
}
