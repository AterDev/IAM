using System.Text.Json;
using CommonMod.Managers;
using EntityFramework.AppDbFactory;
using Share.Exceptions;
using Microsoft.AspNetCore.Http;
using Mapster;
using Entity.IAMMod;
using IAMMod.Models.RoleDtos;

namespace IAMMod.Managers;

/// <summary>
/// Manager for role operations
/// </summary>
public class RoleManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<RoleManager> logger,
    AuditLogManager auditLogManager)
    : ManagerBase<DefaultDbContext, Role>(dbContextFactory, userContext, logger)
{
    private readonly AuditLogManager _auditLogManager = auditLogManager;
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
        // Role management is accessible by admins for now
        // TODO: Implement proper permission checking logic
        // Security safeguard: deny by default until proper permission checks are implemented
        return await Task.FromResult(false);
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
    /// Grant permissions to role
    /// </summary>
    /// <param name="roleId">Role id</param>
    /// <param name="dto">Grant permission DTO</param>
    /// <param name="ipAddress">IP address for audit log</param>
    /// <param name="userAgent">User agent for audit log</param>
    /// <returns>True if successful</returns>
    public async Task<bool> GrantPermissionsAsync(
        Guid roleId,
        RoleGrantPermissionDto dto,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var role = await FindAsync(roleId);
        if (role == null)
        {
            throw new BusinessException("RoleNotFound", StatusCodes.Status404NotFound);
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            // Load current claims
            await LoadManyAsync(role, r => r.RoleClaims);

            // Track changes for audit
            var oldPermissions = role.RoleClaims
                .Select(rc => $"{rc.ClaimType}:{rc.ClaimValue}")
                .ToList();

            // Remove all existing permission claims
            var existingClaims = role.RoleClaims.ToList();
            foreach (var claim in existingClaims)
            {
                _dbContext.Set<RoleClaim>().Remove(claim);
            }

            // Add new claims
            foreach (var permission in dto.Permissions)
            {
                role.RoleClaims.Add(new RoleClaim
                {
                    RoleId = roleId,
                    ClaimType = permission.ClaimType,
                    ClaimValue = permission.ClaimValue
                });
            }

            var result = await _dbContext.SaveChangesAsync() > 0;

            if (result)
            {
                // Write audit log for permission changes
                var newPermissions = dto.Permissions
                    .Select(p => $"{p.ClaimType}:{p.ClaimValue}")
                    .ToList();

                await _auditLogManager.AddAuditLogAsync(
                    category: "Authorization",
                    eventName: "RolePermissionsChanged",
                    subjectId: roleId.ToString(),
                    payload: JsonSerializer.Serialize(new
                    {
                        roleName = role.Name,
                        oldCount = oldPermissions.Count,
                        newCount = newPermissions.Count
                    }),
                    ipAddress: ipAddress,
                    userAgent: userAgent
                );
            }

            return result;
        });
    }

    /// <summary>
    /// Get role permissions
    /// </summary>
    /// <param name="roleId">Role id</param>
    /// <returns>List of permission claims</returns>
    public async Task<List<PermissionClaim>> GetPermissionsAsync(Guid roleId)
    {
        var role = await FindAsync(roleId);
        if (role == null)
        {
            return [];
        }

        await LoadManyAsync(role, r => r.RoleClaims);

        return role.RoleClaims.Select(rc => new PermissionClaim
        {
            ClaimType = rc.ClaimType,
            ClaimValue = rc.ClaimValue ?? string.Empty
        }).ToList();
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
