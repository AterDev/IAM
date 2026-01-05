using IdentityMod.Models.OrganizationDtos;
using EntityFramework.AppDbFactory;
using Share.Exceptions;
using Microsoft.AspNetCore.Http;
using Mapster;

namespace IdentityMod.Managers;

/// <summary>
/// Manager for organization operations
/// </summary>
public class OrganizationManager(
    TenantDbFactory dbContextFactory,
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
        Queryable = Queryable
            .WhereNotNull(filter.Name != null, q => q.Name.Contains(filter.Name!))
            .WhereNotNull(filter.ParentId != null, q => q.ParentId == filter.ParentId)
            .WhereNotNull(filter.Level != null, q => q.Level == filter.Level);

        return await PageListAsync<OrganizationFilterDto, OrganizationItemDto>(filter);
    }

    /// <summary>
    /// Check if user has permission to access organization
    /// </summary>
    /// <param name="id">Organization id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        // Organization management is accessible by admins for now
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Get organization detail by id
    /// </summary>
    /// <param name="id">Organization id</param>
    /// <returns>Organization detail or null</returns>
    public async Task<OrganizationDetailDto?> GetDetailAsync(Guid id)
    {
        return await FindAsync<OrganizationDetailDto>(q => q.Id == id);
    }

    /// <summary>
    /// Add new organization
    /// </summary>
    /// <param name="dto">Organization add DTO</param>
    /// <returns>Created organization detail or null</returns>
    public async Task<OrganizationDetailDto?> AddAsync(OrganizationAddDto dto)
    {
        // Check if name already exists under same parent
        if (await _dbSet.AnyAsync(q => q.Name == dto.Name && q.ParentId == dto.ParentId))
        {
            throw new BusinessException("OrganizationNameExists", StatusCodes.Status400BadRequest);
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            // Get parent info if parent exists
            Organization? parent = null;
            int level = 0;
            string path = "/";

            if (dto.ParentId.HasValue)
            {
                parent = await FindAsync(dto.ParentId.Value);
                if (parent == null)
                {
                    throw new BusinessException("ParentOrganizationNotFound", StatusCodes.Status404NotFound);
                }
                level = parent.Level + 1;
                path = $"{parent.Path}{parent.Id}/";
            }

            var entity = dto.MapTo<Organization>();
            entity.Level = level;
            entity.Path = path;

            await InsertAsync(entity);

            // Update path with actual ID
            entity.Path = dto.ParentId.HasValue ? $"{parent!.Path}{entity.Id}/" : $"/{entity.Id}/";
            entity.UpdatedTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

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

        // Check if name already exists under same parent (if changing)
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != entity.Name)
        {
            if (await _dbSet.AnyAsync(q => q.Name == dto.Name && q.ParentId == entity.ParentId && q.Id != id))
            {
                throw new BusinessException("OrganizationNameExists", StatusCodes.Status400BadRequest);
            }
            entity.Name = dto.Name;
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

            // Update all children paths recursively
            await UpdateChildrenPathsAsync(entity);
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
        await _dbContext.SaveChangesAsync();
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
        if (await _dbSet.AnyAsync(q => q.ParentId == id))
        {
            throw new BusinessException("OrganizationHasChildren", StatusCodes.Status400BadRequest);
        }

        // Check if has users
        await LoadManyAsync(entity, o => o.OrganizationUsers);
        if (entity.OrganizationUsers.Count > 0)
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

        // Load current users
        await LoadManyAsync(organization, o => o.OrganizationUsers);

        // Add new users
        var existingUserIds = organization.OrganizationUsers.Select(ou => ou.UserId).ToList();
        var toAdd = userIds.Where(uid => !existingUserIds.Contains(uid)).ToList();

        foreach (var userId in toAdd)
        {
            organization.OrganizationUsers.Add(new OrganizationUser
            {
                OrganizationId = organizationId,
                UserId = userId
            });
        }

        return await _dbContext.SaveChangesAsync() > 0;
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

        // Load current users
        await LoadManyAsync(organization, o => o.OrganizationUsers);

        // Remove users
        var toRemove = organization.OrganizationUsers
            .Where(ou => userIds.Contains(ou.UserId))
            .ToList();

        foreach (var orgUser in toRemove)
        {
            _dbContext.Set<OrganizationUser>().Remove(orgUser);
        }

        return await _dbContext.SaveChangesAsync() > 0;
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
            await UpdateChildrenPathsAsync(child);
        }

        if (children.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
