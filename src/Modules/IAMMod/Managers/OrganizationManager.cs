using Perigon.PostgreSQL;
using EntityFramework.AppDbFactory;
using Share.Exceptions;
using Microsoft.AspNetCore.Http;
using Entity.IAMMod;
using IAMMod.Models.OrganizationDtos;

namespace IAMMod.Managers;

/// <summary>
/// Manager for organization operations
/// </summary>
public class OrganizationManager(
    AppDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<OrganizationManager> logger
) : ManagerBase<DefaultDbContext, Organization>(dbContextFactory, userContext, logger)
{
    /// <summary>
    /// Get paged organizations
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of organizations</returns>
    public async Task<PageList<OrganizationItemDto>> GetPageAsync(OrganizationFilterDto filter)
    {
        var query = _dbSet
            .WhereNotNull(filter.Name != null, q => q.Name.Contains(filter.Name!))
            .WhereNotNull(filter.ParentId != null, q => q.ParentId == filter.ParentId)
            .WhereNotNull(filter.Level != null, q => q.Level == filter.Level);

        var count = await query.CountAsync();
        var rows = await query.OrderByDescending(item => item.CreatedTime)
            .Skip((filter.PageIndex - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();
        return new PageList<OrganizationItemDto> { Count = count, PageIndex = filter.PageIndex, Data = rows.Select(OrganizationItemDto.FromEntity).ToList() };
    }

    /// <summary>
    /// Check if user has permission to access organization
    /// </summary>
    /// <param name="id">Organization id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        return await Task.FromResult(_userContext.IsAdmin);
    }

    /// <summary>
    /// Get organization detail by id
    /// </summary>
    /// <param name="id">Organization id</param>
    /// <returns>Organization detail or null</returns>
    public async Task<OrganizationDetailDto?> GetDetailAsync(Guid id)
    {
        var entity = await _dbSet.Where(q => q.Id == id).FirstOrDefaultAsync();
        return entity is null ? null : OrganizationDetailDto.FromEntity(entity);
    }

    /// <summary>
    /// Add new organization
    /// </summary>
    /// <param name="dto">Organization add DTO</param>
    /// <returns>Created organization detail or null</returns>
    public async Task<OrganizationDetailDto?> AddAsync(OrganizationAddDto dto)
    {
        // Check if name already exists under same parent
        if (await _dbSet.Where(q => q.Name == dto.Name && q.ParentId == dto.ParentId).AnyAsync())
        {
            throw new BusinessException("OrganizationNameExists", StatusCodes.Status400BadRequest);
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            // Get parent info if parent exists
            Organization? parent = null;
            int level = 0;
            if (dto.ParentId.HasValue)
            {
                parent = await FindAsync(dto.ParentId.Value);
                if (parent == null)
                {
                    throw new BusinessException("ParentOrganizationNotFound", StatusCodes.Status404NotFound);
                }
                level = parent.Level + 1;
            }

            var entity = new Organization { Name = dto.Name, ParentId = dto.ParentId, DisplayOrder = dto.DisplayOrder, Description = dto.Description };
            entity.Level = level;
            entity.Path = parent is null ? $"/{entity.Id}/" : $"{parent.Path}{entity.Id}/";
            await InsertAsync(entity);

            return await GetDetailAsync(entity.Id);
        });
    }

    /// <summary>
    /// Update organization
    /// </summary>
    /// <param name="id">Organization id</param>
    /// <param name="dto">Organization update DTO</param>
    /// <returns>Updated organization detail or null</returns>
    public async Task<OrganizationDetailDto?> UpdateAsync(Guid id, OrganizationUpdateDto dto)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException("OrganizationNotFound", StatusCodes.Status404NotFound);
        }

        // Check the effective target sibling scope before changing any entity values.
        var targetParentId = dto.ParentId ?? entity.ParentId;
        var targetName = string.IsNullOrWhiteSpace(dto.Name) ? entity.Name : dto.Name;
        var nameChanged = !string.IsNullOrWhiteSpace(dto.Name) && dto.Name != entity.Name;
        var parentChanged = dto.ParentId.HasValue && dto.ParentId != entity.ParentId;

        if ((nameChanged || parentChanged)
            && await _dbSet.Where(q => q.Name == targetName && q.ParentId == targetParentId && q.Id != id).AnyAsync())
        {
            throw new BusinessException("OrganizationNameExists", StatusCodes.Status400BadRequest);
        }

        if (nameChanged)
        {
            entity.Name = targetName;
        }

        // Handle parent change
        if (dto.ParentId.HasValue && dto.ParentId != entity.ParentId)
        {
            // Check for circular reference
            if (await IsCircularReferenceAsync(id, dto.ParentId.Value))
            {
                throw new BusinessException("CircularReference", StatusCodes.Status400BadRequest);
            }

            var parent = await FindAsync(dto.ParentId.Value);
            if (parent == null)
            {
                throw new BusinessException("ParentOrganizationNotFound", StatusCodes.Status404NotFound);
            }

            entity.ParentId = dto.ParentId.Value;
            entity.Level = parent.Level + 1;
            entity.Path = $"{parent.Path}{entity.Id}/";

        }

        if (dto.DisplayOrder.HasValue)
        {
            entity.DisplayOrder = dto.DisplayOrder.Value;
        }

        if (dto.Description != null)
        {
            entity.Description = dto.Description;
        }

        entity.UpdatedTime = DateTime.UtcNow;
        await _dbContext.TransactionAsync(async ct =>
        {
            await _dbSet.Where(item => item.Id == entity.Id).ExecuteUpdateAsync(update => update
                .Set(item => item.Name, entity.Name)
                .Set(item => item.ParentId, entity.ParentId)
                .Set(item => item.Level, entity.Level)
                .Set(item => item.Path, entity.Path)
                .Set(item => item.DisplayOrder, entity.DisplayOrder)
                .Set(item => item.Description, entity.Description)
                .Set(item => item.UpdatedTime, entity.UpdatedTime), cancellationToken: ct);
            if (parentChanged)
            {
                await UpdateChildrenPathsAsync(entity);
            }
        });
        return await GetDetailAsync(id);
    }

    /// <summary>
    /// Delete organization (soft delete)
    /// </summary>
    /// <param name="id">Organization id</param>
    /// <param name="softDelete">Perform soft delete (default true)</param>
    /// <returns>True if successful</returns>
    public async Task<bool> DeleteAsync(Guid id, bool softDelete = true)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException("OrganizationNotFound", StatusCodes.Status404NotFound);
        }

        // Check if has children
        if (await _dbSet.Where(q => q.ParentId == id).AnyAsync())
        {
            throw new BusinessException("OrganizationHasChildren", StatusCodes.Status400BadRequest);
        }

        if (await _dbContext.OrganizationUsers.Where(item => item.OrganizationId == id).AnyAsync())
        {
            throw new BusinessException("OrganizationHasUsers", StatusCodes.Status400BadRequest);
        }

        await DeleteOrUpdateAsync([id], softDelete);
        return true;
    }

    /// <summary>
    /// Get organization tree
    /// </summary>
    /// <param name="parentId">Parent organization id (null for root)</param>
    /// <returns>List of organization tree nodes</returns>
    public async Task<List<OrganizationTreeDto>> GetTreeAsync(Guid? parentId = null)
    {
        var organizations = await _dbSet
            .Where(o => o.ParentId == parentId)
            .OrderBy(o => o.DisplayOrder)
            .ThenBy(o => o.Name)
            .ToListAsync();

        var result = new List<OrganizationTreeDto>();

        foreach (var org in organizations)
        {
            var treeNode = new OrganizationTreeDto
            {
                Id = org.Id,
                Name = org.Name,
                ParentId = org.ParentId,
                Level = org.Level,
                DisplayOrder = org.DisplayOrder,
                Description = org.Description,
                Children = await GetTreeAsync(org.Id)
            };
            result.Add(treeNode);
        }

        return result;
    }

    /// <summary>
    /// Add users to organization
    /// </summary>
    /// <param name="organizationId">Organization id</param>
    /// <param name="userIds">User ids to add</param>
    /// <returns>True if successful</returns>
    public async Task<bool> AddUsersAsync(Guid organizationId, List<Guid> userIds)
    {
        var organization = await FindAsync(organizationId);
        if (organization == null)
        {
            throw new BusinessException("OrganizationNotFound", StatusCodes.Status404NotFound);
        }

        var existing = await _dbContext.OrganizationUsers
            .Where(item => item.OrganizationId == organizationId).Select(item => item.UserId).ToScalarListAsync();
        var additions = userIds.Distinct().Except(existing)
            .Select(userId => new OrganizationUser { OrganizationId = organizationId, UserId = userId }).ToArray();
        if (additions.Length == 0)
        {
            return false;
        }
        await _dbContext.OrganizationUsers.BulkInsertAsync(additions);
        return true;
    }

    /// <summary>
    /// Remove users from organization
    /// </summary>
    /// <param name="organizationId">Organization id</param>
    /// <param name="userIds">User ids to remove</param>
    /// <returns>True if successful</returns>
    public async Task<bool> RemoveUsersAsync(Guid organizationId, List<Guid> userIds)
    {
        var organization = await FindAsync(organizationId);
        if (organization == null)
        {
            throw new BusinessException("OrganizationNotFound", StatusCodes.Status404NotFound);
        }

        return await _dbContext.OrganizationUsers
            .Where(item => item.OrganizationId == organizationId && Enumerable.Contains(userIds, item.UserId))
            .ExecuteDeleteAsync() > 0;
    }

    /// <summary>
    /// Check if moving organization would create circular reference
    /// </summary>
    private async Task<bool> IsCircularReferenceAsync(Guid organizationId, Guid newParentId)
    {
        if (organizationId == newParentId)
        {
            return true;
        }

        var parent = await FindAsync(newParentId);
        while (parent != null && parent.ParentId.HasValue)
        {
            if (parent.ParentId == organizationId)
            {
                return true;
            }
            parent = await FindAsync(parent.ParentId.Value);
        }

        return false;
    }

    /// <summary>
    /// Update paths for all children recursively
    /// </summary>
    private async Task UpdateChildrenPathsAsync(Organization organization)
    {
        var children = await _dbSet
            .Where(o => o.ParentId == organization.Id)
            .ToListAsync();

        foreach (var child in children)
        {
            child.Level = organization.Level + 1;
            child.Path = $"{organization.Path}{child.Id}/";
            await _dbSet.Where(item => item.Id == child.Id).ExecuteUpdateAsync(update => update
                .Set(item => item.Level, child.Level).Set(item => item.Path, child.Path));
            await UpdateChildrenPathsAsync(child);
        }

    }
}
