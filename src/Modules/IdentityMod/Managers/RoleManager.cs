using CommonMod.Managers;
using Entity.IdentityMod;
using EntityFramework.DBProvider;
using IdentityMod.Models.RoleDtos;
using System.Text.Json;

namespace IdentityMod.Managers;

/// <summary>
/// Manager for role operations
/// </summary>
public class RoleManager(
    DefaultDbContext dbContext,
    ILogger<RoleManager> logger,
    AuditLogManager auditLogManager)
    : ManagerBase<DefaultDbContext, Role>(dbContext, logger)
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
            .WhereNotNull(filter.Name != null, q => q.Name.Contains(filter.Name!))
            .WhereNotNull(filter.StartDate != null, q => q.CreatedTime >= filter.StartDate)
            .WhereNotNull(filter.EndDate != null, q => q.CreatedTime <= filter.EndDate);

        return await ToPageAsync<RoleFilterDto, RoleItemDto>(filter);
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
        if (await ExistAsync(q => q.NormalizedName == normalizedName))
        {
            ErrorMsg = "Role name already exists";
            return null;
        }

        var entity = new Role
        {
            Name = dto.Name,
            NormalizedName = normalizedName,
            Description = dto.Description,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        var success = await AddAsync(entity);
        return !success ? null : await GetDetailAsync(entity.Id);
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
            ErrorMsg = "Role not found";
            return null;
        }

        // Check if name already exists (if changing)
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != entity.Name)
        {
            var normalizedName = dto.Name.ToUpperInvariant();
            if (await ExistAsync(q => q.NormalizedName == normalizedName && q.Id != id))
            {
                ErrorMsg = "Role name already exists";
                return null;
            }
            entity.Name = dto.Name;
            entity.NormalizedName = normalizedName;
        }

        if (dto.Description != null)
        {
            entity.Description = dto.Description;
        }

        entity.ConcurrencyStamp = Guid.NewGuid().ToString();

        var success = await UpdateAsync(entity);
        return !success ? null : await GetDetailAsync(id);
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
            ErrorMsg = "Role not found";
            return false;
        }

        // Check if role has users
        await LoadManyAsync(entity, r => r.UserRoles);
        if (entity.UserRoles.Count > 0)
        {
            ErrorMsg = "Cannot delete role that has users assigned";
            return false;
        }

        return await DeleteAsync(entity, softDelete);
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
            ErrorMsg = "Role not found";
            return false;
        }

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

        var result = await SaveChangesAsync() > 0;

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
        return await ToListAsync<RoleItemDto>();
    }
}
