using Entity.Identity;
using EntityFramework.DBProvider;
using IdentityMod.Models.OrganizationDtos;

namespace IdentityMod.Managers;

/// <summary>
/// Manager for organization operations
/// </summary>
public class OrganizationManager(DefaultDbContext dbContext, ILogger<OrganizationManager> logger)
    : ManagerBase<DefaultDbContext, Organization>(dbContext, logger)
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

        return await ToPageAsync<OrganizationFilterDto, OrganizationItemDto>(filter);
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
        if (await ExistAsync(q => q.Name == dto.Name && q.ParentId == dto.ParentId))
        {
            ErrorMsg = "Organization name already exists under the same parent";
            return null;
        }

        // Get parent info if parent exists
        Organization? parent = null;
        int level = 0;
        string path = "/";

        if (dto.ParentId.HasValue)
        {
            parent = await FindAsync(dto.ParentId.Value);
            if (parent == null)
            {
                ErrorMsg = "Parent organization not found";
                return null;
            }
            level = parent.Level + 1;
            path = $"{parent.Path}{parent.Id}/";
        }

        var entity = new Organization
        {
            Name = dto.Name,
            ParentId = dto.ParentId,
            Level = level,
            Path = path,
            DisplayOrder = dto.DisplayOrder,
            Description = dto.Description
        };

        var success = await AddAsync(entity);
        if (!success)
        {
            return null;
        }

        // Update path with actual ID
        entity.Path = dto.ParentId.HasValue ? $"{parent!.Path}{entity.Id}/" : $"/{entity.Id}/";
        await UpdateAsync(entity);

        return await GetDetailAsync(entity.Id);
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
            ErrorMsg = "Organization not found";
            return null;
        }

        // Check if name already exists under same parent (if changing)
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != entity.Name)
        {
            if (await ExistAsync(q => q.Name == dto.Name && q.ParentId == entity.ParentId && q.Id != id))
            {
                ErrorMsg = "Organization name already exists under the same parent";
                return null;
            }
            entity.Name = dto.Name;
        }

        // Handle parent change
        if (dto.ParentId.HasValue && dto.ParentId != entity.ParentId)
        {
            // Check for circular reference
            if (await IsCircularReferenceAsync(id, dto.ParentId.Value))
            {
                ErrorMsg = "Cannot move organization to its own descendant";
                return false;
            }

            var parent = await FindAsync(dto.ParentId.Value);
            if (parent == null)
            {
                ErrorMsg = "Parent organization not found";
                return null;
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

        var success = await UpdateAsync(entity);
        return !success ? null : await GetDetailAsync(id);
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
            ErrorMsg = "Organization not found";
            return false;
        }

        // Check if has children
        if (await ExistAsync(q => q.ParentId == id))
        {
            ErrorMsg = "Cannot delete organization with children. Please delete or move children first.";
            return false;
        }

        // Check if has users
        await LoadManyAsync(entity, o => o.OrganizationUsers);
        if (entity.OrganizationUsers.Count > 0)
        {
            ErrorMsg = "Cannot delete organization with users. Please remove users first.";
            return false;
        }

        return await DeleteAsync(entity, softDelete);
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
            ErrorMsg = "Organization not found";
            return false;
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

        return await SaveChangesAsync() > 0;
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
            ErrorMsg = "Organization not found";
            return false;
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

        return await SaveChangesAsync() > 0;
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
            await SaveChangesAsync();
        }
    }
}
