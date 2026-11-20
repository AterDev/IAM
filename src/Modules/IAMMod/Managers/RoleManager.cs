using Perigon.PostgreSQL;
using IAMMod.Models.RoleDtos;
using Microsoft.AspNetCore.Http;
using Share.Exceptions;

namespace IAMMod.Managers;

/// <summary>
/// Manager for role operations
/// </summary>
public class RoleManager(
    AppDbFactory dbContextFactory,
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
        var query = _dbSet
            .WhereNotNull(filter.Name, q => q.Name.Contains(filter.Name!))
            .WhereNotNull(filter.StartDate, q => q.CreatedTime >= filter.StartDate)
            .WhereNotNull(filter.EndDate, q => q.CreatedTime <= filter.EndDate);

        var count = await query.CountAsync();
        var rows = await query.OrderByDescending(item => item.CreatedTime)
            .Skip((filter.PageIndex - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();
        return new PageList<RoleItemDto> { Count = count, PageIndex = filter.PageIndex, Data = rows.Select(RoleItemDto.FromEntity).ToList() };
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
        var entity = await _dbSet.Where(q => q.Id == id).FirstOrDefaultAsync();
        return entity is null ? null : RoleDetailDto.FromEntity(entity);
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    /// <param name="name">Role name</param>
    /// <returns>Role detail or null</returns>
    public async Task<RoleDetailDto?> GetByNameAsync(string name)
    {
        var normalizedName = name.ToUpperInvariant();
        var entity = await _dbSet.Where(q => q.NormalizedName == normalizedName).FirstOrDefaultAsync();
        return entity is null ? null : RoleDetailDto.FromEntity(entity);
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
        if (await _dbSet.Where(q => q.NormalizedName == normalizedName).AnyAsync())
        {
            throw new BusinessException("RoleNameExists", StatusCodes.Status400BadRequest);
        }

        var entity = new Role { Name = dto.Name, Description = dto.Description };
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
            if (await _dbSet.Where(q => q.NormalizedName == normalizedName && q.Id != id).AnyAsync())
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

        var priorStamp = entity.ConcurrencyStamp;
        entity.ConcurrencyStamp = Guid.NewGuid().ToString();
        entity.UpdatedTime = DateTime.UtcNow;

        var updated = await _dbSet.Where(item => item.Id == id && item.ConcurrencyStamp == priorStamp)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.Name, entity.Name)
                .Set(item => item.NormalizedName, entity.NormalizedName)
                .Set(item => item.Description, entity.Description)
                .Set(item => item.ConcurrencyStamp, entity.ConcurrencyStamp)
                .Set(item => item.UpdatedTime, entity.UpdatedTime));
        if (updated == 0)
        {
            throw new BusinessException(Localizer.AlreadyUpdated, StatusCodes.Status409Conflict);
        }
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
        if (await _dbContext.UserRoles.Where(item => item.RoleId == id).AnyAsync())
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
        return (await _dbSet.ToListAsync()).Select(RoleItemDto.FromEntity).ToList();
    }

    /// <summary>
    /// Get role names by IDs
    /// </summary>
    /// <param name="roleIds">List of role IDs</param>
    /// <returns>List of role names</returns>
    public async Task<List<string>> GetRoleNamesByIdsAsync(List<Guid> roleIds)
    {
        return await Queryable
            .Where(r => Enumerable.Contains(roleIds, r.Id))
            .Select(r => r.Name)
            .ToScalarListAsync();
    }
}
