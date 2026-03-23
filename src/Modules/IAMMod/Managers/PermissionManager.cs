using IAMMod.Models.PermissionDtos;
using IAMMod.Models.RoleDtos;
using Microsoft.AspNetCore.Http;
using Share.Exceptions;
using System.Text.Json;

namespace IAMMod.Managers;

/// <summary>
/// Manager for the unified permission model.
/// </summary>
public class PermissionManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<PermissionManager> logger,
    AuditLogManager auditLogManager)
    : ManagerBase<DefaultDbContext, Permission>(dbContextFactory, userContext, logger)
{
    private readonly AuditLogManager _auditLogManager = auditLogManager;

    public override Task<bool> HasPermissionAsync(Guid id)
    {
        return Task.FromResult(_userContext.IsAdmin);
    }

    /// <summary>
    /// Get paged permissions.
    /// </summary>
    public async Task<PageList<PermissionItemDto>> GetPageAsync(PermissionFilterDto filter)
    {
        var clientId = await ResolveClientIdAsync(filter.ClientId, filter.ClientCode);
        var query = BuildFilteredPermissionQuery(filter, clientId)
            .Select(permission => new PermissionItemDto
            {
                Id = permission.Id,
                Code = permission.Code,
                Name = permission.Name,
                DisplayName = permission.DisplayName,
                Description = permission.Description,
                Type = permission.Type,
                ParentId = permission.ParentId,
                ParentCode = permission.Parent != null ? permission.Parent.Code : null,
                Namespace = permission.Namespace,
                Resource = permission.Resource,
                Action = permission.Action,
                Path = permission.Path,
                Icon = permission.Icon,
                Sort = permission.Sort,
                OwnedClientId = permission.OwnedClientId,
                OwnedClientCode = permission.OwnedClient != null ? permission.OwnedClient.ClientId : null,
                CreatedTime = permission.CreatedTime,
                UpdatedTime = permission.UpdatedTime,
            });

        query = filter.OrderBy != null && filter.OrderBy.Count > 0
            ? query.OrderBy(filter.OrderBy)
            : query.OrderBy(permission => permission.Sort).ThenBy(permission => permission.Code);

        var count = await query.CountAsync();
        var data = await query
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PageList<PermissionItemDto>
        {
            Count = count,
            Data = data,
            PageIndex = filter.PageIndex,
        };
    }

    /// <summary>
    /// Get permission detail.
    /// </summary>
    public async Task<PermissionDetailDto?> GetDetailAsync(Guid id)
    {
        return await _dbContext.Permissions
            .AsNoTracking()
            .Include(permission => permission.Parent)
            .Include(permission => permission.OwnedClient)
            .Where(permission => permission.Id == id)
            .Select(permission => new PermissionDetailDto
            {
                Id = permission.Id,
                Code = permission.Code,
                Name = permission.Name,
                DisplayName = permission.DisplayName,
                Description = permission.Description,
                Type = permission.Type,
                ParentId = permission.ParentId,
                ParentCode = permission.Parent != null ? permission.Parent.Code : null,
                Namespace = permission.Namespace,
                Resource = permission.Resource,
                Action = permission.Action,
                Path = permission.Path,
                Icon = permission.Icon,
                Sort = permission.Sort,
                OwnedClientId = permission.OwnedClientId,
                OwnedClientCode = permission.OwnedClient != null ? permission.OwnedClient.ClientId : null,
                CreatedTime = permission.CreatedTime,
                UpdatedTime = permission.UpdatedTime,
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get permission tree.
    /// </summary>
    public async Task<List<PermissionTreeNodeDto>> GetTreeAsync(PermissionFilterDto filter)
    {
        var clientId = await ResolveClientIdAsync(filter.ClientId, filter.ClientCode);
        var items = await BuildFilteredPermissionQuery(filter, clientId)
            .OrderBy(permission => permission.Sort)
            .ThenBy(permission => permission.Code)
            .Select(permission => new PermissionTreeNodeDto
            {
                Id = permission.Id,
                Code = permission.Code,
                Name = permission.Name,
                DisplayName = permission.DisplayName,
                Description = permission.Description,
                Type = permission.Type,
                ParentId = permission.ParentId,
                Namespace = permission.Namespace,
                Resource = permission.Resource,
                Action = permission.Action,
                Path = permission.Path,
                Icon = permission.Icon,
                Sort = permission.Sort,
                OwnedClientId = permission.OwnedClientId,
                OwnedClientCode = permission.OwnedClient != null ? permission.OwnedClient.ClientId : null,
            })
            .ToListAsync();

        return BuildTree(items, []);
    }

    /// <summary>
    /// Get permission tree with role selection state.
    /// </summary>
    public async Task<List<PermissionTreeNodeDto>> GetRolePermissionTreeAsync(Guid roleId, PermissionFilterDto filter)
    {
        var selectedCodes = await _dbContext.RolePermissions
            .Where(item => item.RoleId == roleId)
            .Select(item => item.Permission.Code)
            .ToListAsync();

        var clientId = await ResolveClientIdAsync(filter.ClientId, filter.ClientCode);
        var items = await BuildFilteredPermissionQuery(filter, clientId)
            .OrderBy(permission => permission.Sort)
            .ThenBy(permission => permission.Code)
            .Select(permission => new PermissionTreeNodeDto
            {
                Id = permission.Id,
                Code = permission.Code,
                Name = permission.Name,
                DisplayName = permission.DisplayName,
                Description = permission.Description,
                Type = permission.Type,
                ParentId = permission.ParentId,
                Namespace = permission.Namespace,
                Resource = permission.Resource,
                Action = permission.Action,
                Path = permission.Path,
                Icon = permission.Icon,
                Sort = permission.Sort,
                OwnedClientId = permission.OwnedClientId,
                OwnedClientCode = permission.OwnedClient != null ? permission.OwnedClient.ClientId : null,
            })
            .ToListAsync();

        return BuildTree(items, selectedCodes);
    }

    /// <summary>
    /// Get permission tree with client selection state.
    /// </summary>
    public async Task<List<PermissionTreeNodeDto>> GetClientPermissionTreeAsync(Guid clientId, PermissionFilterDto filter)
    {
        filter.ClientId = clientId;

        var selectedCodes = await _dbContext.ClientPermissions
            .Where(item => item.ClientId == clientId)
            .Select(item => item.Permission.Code)
            .ToListAsync();

        var items = await BuildFilteredPermissionQuery(filter, clientId)
            .OrderBy(permission => permission.Sort)
            .ThenBy(permission => permission.Code)
            .Select(permission => new PermissionTreeNodeDto
            {
                Id = permission.Id,
                Code = permission.Code,
                Name = permission.Name,
                DisplayName = permission.DisplayName,
                Description = permission.Description,
                Type = permission.Type,
                ParentId = permission.ParentId,
                Namespace = permission.Namespace,
                Resource = permission.Resource,
                Action = permission.Action,
                Path = permission.Path,
                Icon = permission.Icon,
                Sort = permission.Sort,
                OwnedClientId = permission.OwnedClientId,
                OwnedClientCode = permission.OwnedClient != null ? permission.OwnedClient.ClientId : null,
            })
            .ToListAsync();

        return BuildTree(items, selectedCodes);
    }

    /// <summary>
    /// Get the current user's permitted menu tree for the specified client.
    /// </summary>
    public async Task<List<PermissionTreeNodeDto>> GetCurrentUserMenuTreeAsync(Guid userId, string clientCode)
    {
        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ClientId == clientCode);

        if (client == null)
        {
            return [];
        }

        var roleIds = await _dbContext.UserRoles
            .Where(item => item.UserId == userId)
            .Select(item => item.RoleId)
            .Distinct()
            .ToListAsync();

        if (roleIds.Count == 0)
        {
            return [];
        }

        var selectedPermissionIds = await _dbContext.RolePermissions
            .Where(item => roleIds.Contains(item.RoleId))
            .Select(item => item.PermissionId)
            .Distinct()
            .ToListAsync();

        if (selectedPermissionIds.Count == 0)
        {
            return [];
        }

        var clientPermissionIds = await _dbContext.ClientPermissions
            .Where(item => item.ClientId == client.Id)
            .Select(item => item.PermissionId)
            .Distinct()
            .ToListAsync();

        if (clientPermissionIds.Count == 0)
        {
            return [];
        }

        var allowedPermissionIds = selectedPermissionIds
            .Intersect(clientPermissionIds)
            .ToHashSet();

        var items = await _dbContext.Permissions
            .AsNoTracking()
            .Include(permission => permission.OwnedClient)
            .Where(permission => allowedPermissionIds.Contains(permission.Id))
            .Where(permission => permission.Type == PermissionType.Menu || permission.Type == PermissionType.Button)
            .OrderBy(permission => permission.Sort)
            .ThenBy(permission => permission.Code)
            .Select(permission => new PermissionTreeNodeDto
            {
                Id = permission.Id,
                Code = permission.Code,
                Name = permission.Name,
                DisplayName = permission.DisplayName,
                Description = permission.Description,
                Type = permission.Type,
                ParentId = permission.ParentId,
                Namespace = permission.Namespace,
                Resource = permission.Resource,
                Action = permission.Action,
                Path = permission.Path,
                Icon = permission.Icon,
                Sort = permission.Sort,
                OwnedClientId = permission.OwnedClientId,
                OwnedClientCode = permission.OwnedClient != null ? permission.OwnedClient.ClientId : null,
            })
            .ToListAsync();

        return BuildTree(items, []);
    }

    /// <summary>
    /// Create a permission.
    /// </summary>
    public async Task<PermissionDetailDto?> AddAsync(PermissionUpsertDto dto)
    {
        await ValidatePermissionUpsertAsync(dto);

        var entity = new Permission
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
        };
        ApplyPermissionValues(entity, dto);
        entity.TenantId = GetTenantId();

        await _dbContext.Permissions.AddAsync(entity);

        if (dto.OwnedClientId.HasValue)
        {
            await EnsureClientPermissionAsync(dto.OwnedClientId.Value, entity);
        }

        await _dbContext.SaveChangesAsync();
        return await GetDetailAsync(entity.Id);
    }

    /// <summary>
    /// Update a permission.
    /// </summary>
    public async Task<PermissionDetailDto?> UpdateAsync(Guid id, PermissionUpsertDto dto)
    {
        var entity = await _dbContext.Permissions
            .Include(permission => permission.ClientPermissions)
            .FirstOrDefaultAsync(permission => permission.Id == id);

        if (entity == null)
        {
            throw new BusinessException("PermissionNotFound", StatusCodes.Status404NotFound);
        }

        await ValidatePermissionUpsertAsync(dto, id);

        var oldOwnedClientId = entity.OwnedClientId;
        ApplyPermissionValues(entity, dto);
        entity.UpdatedTime = DateTimeOffset.UtcNow;

        if (oldOwnedClientId != dto.OwnedClientId)
        {
            if (oldOwnedClientId.HasValue)
            {
                await _dbContext.ClientPermissions
                    .Where(item => item.ClientId == oldOwnedClientId.Value && item.PermissionId == entity.Id)
                    .ExecuteDeleteAsync();
            }

            if (dto.OwnedClientId.HasValue)
            {
                await EnsureClientPermissionAsync(dto.OwnedClientId.Value, entity);
            }
        }

        await _dbContext.SaveChangesAsync();
        return await GetDetailAsync(id);
    }

    /// <summary>
    /// Delete a permission and its descendants.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var permissionIds = await CollectPermissionIdsAsync(id);
        if (permissionIds.Count == 0)
        {
            throw new BusinessException("PermissionNotFound", StatusCodes.Status404NotFound);
        }

        await _dbContext.RolePermissions
            .Where(item => permissionIds.Contains(item.PermissionId))
            .ExecuteDeleteAsync();

        await _dbContext.ClientPermissions
            .Where(item => permissionIds.Contains(item.PermissionId))
            .ExecuteDeleteAsync();

        await _dbContext.Permissions
            .Where(item => permissionIds.Contains(item.Id))
            .ExecuteDeleteAsync();

        return true;
    }

    /// <summary>
    /// Replace role permissions.
    /// </summary>
    public async Task<bool> GrantRolePermissionsAsync(
        Guid roleId,
        RoleGrantPermissionDto dto,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(item => item.Id == roleId);
        if (role == null)
        {
            throw new BusinessException("RoleNotFound", StatusCodes.Status404NotFound);
        }

        var permissionCodes = NormalizePermissionCodes(dto.PermissionCodes);
        var permissions = permissionCodes.Count == 0
            ? []
            : await _dbContext.Permissions
                .Where(item => permissionCodes.Contains(item.Code))
                .Select(item => new { item.Id, item.Code })
                .ToListAsync();

        if (permissions.Count != permissionCodes.Count)
        {
            throw new BusinessException("PermissionNotFound", StatusCodes.Status404NotFound);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        await _dbContext.RolePermissions.Where(item => item.RoleId == roleId).ExecuteDeleteAsync();

        if (permissions.Count > 0)
        {
            var entities = permissions.Select(item => new RolePermission
            {
                RoleId = roleId,
                PermissionId = item.Id,
                TenantId = GetTenantId(),
            }).ToList();

            await _dbContext.RolePermissions.AddRangeAsync(entities);
        }

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditLogManager.AddAuditLogAsync(
            category: "Authorization",
            eventName: "RolePermissionsChanged",
            subjectId: roleId.ToString(),
            payload: JsonSerializer.Serialize(new
            {
                role.Name,
                count = permissionCodes.Count,
                permissionCodes,
            }),
            ipAddress: ipAddress,
            userAgent: userAgent);

        return true;
    }

    /// <summary>
    /// Get role permission codes.
    /// </summary>
    public async Task<List<string>> GetRolePermissionCodesAsync(Guid roleId)
    {
        var permissionCodes = await _dbContext.RolePermissions
            .Where(item => item.RoleId == roleId)
            .Select(item => item.Permission.Code)
            .OrderBy(item => item)
            .ToListAsync();

        return permissionCodes;
    }

    /// <summary>
    /// Replace client permissions.
    /// </summary>
    public async Task<bool> AssignClientPermissionsAsync(Guid clientId, IEnumerable<string>? permissionCodes)
    {
        var client = await _dbContext.Clients.FirstOrDefaultAsync(item => item.Id == clientId);
        if (client == null)
        {
            throw new BusinessException(Localizer.ClientNotFound, StatusCodes.Status404NotFound);
        }

        var normalizedPermissionCodes = NormalizePermissionCodes(permissionCodes);
        var permissions = normalizedPermissionCodes.Count == 0
            ? []
            : await _dbContext.Permissions
                .Where(item => normalizedPermissionCodes.Contains(item.Code))
                .Select(item => new { item.Id, item.Code })
                .ToListAsync();

        if (permissions.Count != normalizedPermissionCodes.Count)
        {
            throw new BusinessException("PermissionNotFound", StatusCodes.Status404NotFound);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        await _dbContext.ClientPermissions.Where(item => item.ClientId == clientId).ExecuteDeleteAsync();

        if (permissions.Count > 0)
        {
            var entities = permissions.Select(item => new ClientPermission
            {
                ClientId = clientId,
                PermissionId = item.Id,
                TenantId = GetTenantId(),
            }).ToList();

            await _dbContext.ClientPermissions.AddRangeAsync(entities);
        }

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    /// <summary>
    /// Get client permission codes.
    /// </summary>
    public async Task<List<string>> GetClientPermissionCodesAsync(Guid clientId)
    {
        var permissionCodes = await _dbContext.ClientPermissions
            .Where(item => item.ClientId == clientId)
            .Select(item => item.Permission.Code)
            .OrderBy(item => item)
            .ToListAsync();

        return permissionCodes;
    }

    /// <summary>
    /// Synchronize client menu/button permissions with full replacement.
    /// </summary>
    public async Task<bool> SyncClientMenuPermissionsAsync(
        Guid clientId,
        ClientPermissionSyncDto dto,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var client = await _dbContext.Clients.FirstOrDefaultAsync(item => item.Id == clientId);
        if (client == null)
        {
            throw new BusinessException(Localizer.ClientNotFound, StatusCodes.Status404NotFound);
        }

        ValidateSyncTree(dto.Permissions);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var ownedPermissionIds = await _dbContext.Permissions
            .Where(item => item.OwnedClientId == clientId)
            .Where(item => item.Type == PermissionType.Menu || item.Type == PermissionType.Button)
            .Select(item => item.Id)
            .ToListAsync();

        var linkedPermissionIds = await _dbContext.ClientPermissions
            .Where(item => item.ClientId == clientId)
            .Where(item => item.Permission.Type == PermissionType.Menu || item.Permission.Type == PermissionType.Button)
            .Select(item => item.PermissionId)
            .Distinct()
            .ToListAsync();

        var scopedPermissionIds = ownedPermissionIds
            .Concat(linkedPermissionIds)
            .Distinct()
            .ToList();

        if (linkedPermissionIds.Count > 0)
        {
            await _dbContext.ClientPermissions
                .Where(item => item.ClientId == clientId)
                .Where(item => linkedPermissionIds.Contains(item.PermissionId))
                .Where(item => item.Permission.Type == PermissionType.Menu || item.Permission.Type == PermissionType.Button)
                .ExecuteDeleteAsync();
        }

        if (scopedPermissionIds.Count > 0)
        {
            var orphanPermissionIds = await _dbContext.Permissions
                .Where(item => scopedPermissionIds.Contains(item.Id))
                .Where(item => item.Type == PermissionType.Menu || item.Type == PermissionType.Button)
                .Where(item => !item.ClientPermissions.Any())
                .Select(item => item.Id)
                .ToListAsync();

            if (orphanPermissionIds.Count > 0)
            {
                await _dbContext.RolePermissions
                    .Where(item => orphanPermissionIds.Contains(item.PermissionId))
                    .ExecuteDeleteAsync();

                var deletablePermissionIds = await _dbContext.Permissions
                    .Where(item => orphanPermissionIds.Contains(item.Id))
                    .Where(item => item.OwnedClientId == clientId)
                    .Select(item => item.Id)
                    .ToListAsync();

                if (deletablePermissionIds.Count > 0)
                {
                    await _dbContext.Permissions
                        .Where(item => deletablePermissionIds.Contains(item.Id))
                        .ExecuteDeleteAsync();
                }
            }
        }

        if (ownedPermissionIds.Count > 0)
        {
            await _dbContext.Permissions
                .Where(item => ownedPermissionIds.Contains(item.Id))
                .ExecuteDeleteAsync();
        }

        var createdPermissions = new List<Permission>();
        var createdRelations = new List<ClientPermission>();
        foreach (var node in dto.Permissions.OrderBy(item => item.Sort).ThenBy(item => item.Code))
        {
            await CreateClientPermissionNodeAsync(clientId, node, null, createdPermissions, createdRelations);
        }

        if (createdPermissions.Count > 0)
        {
            await _dbContext.Permissions.AddRangeAsync(createdPermissions);
            await _dbContext.ClientPermissions.AddRangeAsync(createdRelations);
        }

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditLogManager.AddAuditLogAsync(
            category: "Authorization",
            eventName: "ClientMenuPermissionsSynced",
            subjectId: clientId.ToString(),
            payload: JsonSerializer.Serialize(new
            {
                client.ClientId,
                count = createdPermissions.Count,
            }),
            ipAddress: ipAddress,
            userAgent: userAgent);

        return true;
    }

    private async Task<Guid?> ResolveClientIdAsync(Guid? clientId, string? clientCode)
    {
        if (clientId.HasValue)
        {
            return clientId;
        }

        if (string.IsNullOrWhiteSpace(clientCode))
        {
            return null;
        }

        return await _dbContext.Clients
            .Where(item => item.ClientId == clientCode)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync();
    }

    private IQueryable<Permission> BuildFilteredPermissionQuery(PermissionFilterDto filter, Guid? clientId)
    {
        var query = _dbContext.Permissions
            .AsNoTracking()
            .Include(permission => permission.Parent)
            .Include(permission => permission.OwnedClient)
            .Include(permission => permission.ClientPermissions)
            .AsQueryable();

        query = query
            .WhereNotNull(filter.Type, permission => permission.Type == filter.Type)
            .WhereNotNull(filter.ParentId, permission => permission.ParentId == filter.ParentId)
            .WhereNotNull(
                filter.OnlyNonBusiness == true,
                permission => permission.Type == PermissionType.Menu || permission.Type == PermissionType.Button)
            .WhereNotNull(
                filter.Keyword,
                permission =>
                    permission.Code.Contains(filter.Keyword!)
                    || permission.Name.Contains(filter.Keyword!)
                    || (permission.DisplayName != null && permission.DisplayName.Contains(filter.Keyword!))
                    || (permission.Description != null && permission.Description.Contains(filter.Keyword!)));

        if (clientId.HasValue)
        {
            query = query.Where(permission => permission.ClientPermissions.Any(item => item.ClientId == clientId.Value));
        }

        return query;
    }

    private static List<string> NormalizePermissionCodes(IEnumerable<string>? permissionCodes)
    {
        return permissionCodes?
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList()
            ?? [];
    }

    private async Task ValidatePermissionUpsertAsync(PermissionUpsertDto dto, Guid? currentId = null)
    {
        if (await _dbContext.Permissions.AnyAsync(item => item.Code == dto.Code && item.Id != currentId))
        {
            throw new BusinessException("PermissionCodeExists", StatusCodes.Status400BadRequest);
        }

        if (dto.ParentId.HasValue)
        {
            var parent = await _dbContext.Permissions.FirstOrDefaultAsync(item => item.Id == dto.ParentId.Value);
            if (parent == null)
            {
                throw new BusinessException("PermissionParentNotFound", StatusCodes.Status404NotFound);
            }

            if (currentId.HasValue && parent.Id == currentId.Value)
            {
                throw new BusinessException("PermissionParentInvalid", StatusCodes.Status400BadRequest);
            }
        }

        if (dto.OwnedClientId.HasValue)
        {
            var clientExists = await _dbContext.Clients.AnyAsync(item => item.Id == dto.OwnedClientId.Value);
            if (!clientExists)
            {
                throw new BusinessException(Localizer.ClientNotFound, StatusCodes.Status404NotFound);
            }
        }
    }

    private static void ApplyPermissionValues(Permission entity, PermissionUpsertDto dto)
    {
        entity.Code = dto.Code.Trim();
        entity.Name = dto.Name.Trim();
        entity.DisplayName = dto.DisplayName?.Trim();
        entity.Description = dto.Description?.Trim();
        entity.Type = dto.Type;
        entity.ParentId = dto.ParentId;
        entity.Namespace = dto.Namespace?.Trim();
        entity.Resource = dto.Resource?.Trim();
        entity.Action = dto.Action?.Trim();
        entity.Path = dto.Path?.Trim();
        entity.Icon = dto.Icon?.Trim();
        entity.Sort = dto.Sort;
        entity.OwnedClientId = dto.OwnedClientId;
    }

    private async Task EnsureClientPermissionAsync(Guid clientId, Permission permission)
    {
        var exists = permission.ClientPermissions.Any(item => item.ClientId == clientId)
            || await _dbContext.ClientPermissions.AnyAsync(item => item.ClientId == clientId && item.PermissionId == permission.Id);

        if (!exists)
        {
            permission.ClientPermissions.Add(new ClientPermission
            {
                ClientId = clientId,
                Permission = permission,
                TenantId = GetTenantId(),
            });
        }
    }

    private async Task<List<Guid>> CollectPermissionIdsAsync(Guid rootId)
    {
        var existingIds = await _dbContext.Permissions
            .AsNoTracking()
            .Select(item => new { item.Id, item.ParentId })
            .ToListAsync();

        if (existingIds.All(item => item.Id != rootId))
        {
            return [];
        }

        var childrenLookup = existingIds
            .Where(item => item.ParentId.HasValue)
            .GroupBy(item => item.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Id).ToList());

        var result = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (result.Contains(current))
            {
                continue;
            }

            result.Add(current);
            if (!childrenLookup.TryGetValue(current, out var children))
            {
                continue;
            }

            foreach (var childId in children)
            {
                queue.Enqueue(childId);
            }
        }

        return result;
    }

    private static List<PermissionTreeNodeDto> BuildTree(
        List<PermissionTreeNodeDto> nodes,
        IReadOnlyCollection<string> selectedCodes)
    {
        var selectedSet = selectedCodes.ToHashSet(StringComparer.Ordinal);
        var lookup = nodes.ToDictionary(node => node.Id);
        foreach (var node in nodes)
        {
            node.Children = [];
            node.Selected = selectedSet.Contains(node.Code);
        }

        var roots = new List<PermissionTreeNodeDto>();
        foreach (var node in nodes.OrderBy(item => item.Sort).ThenBy(item => item.Code))
        {
            if (node.ParentId.HasValue && lookup.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        SortTree(roots);
        return roots;
    }

    private static void SortTree(List<PermissionTreeNodeDto> nodes)
    {
        nodes.Sort((left, right) => left.Sort != right.Sort
            ? left.Sort.CompareTo(right.Sort)
            : string.Compare(left.Code, right.Code, StringComparison.Ordinal));

        foreach (var node in nodes)
        {
            if (node.Children.Count > 0)
            {
                SortTree(node.Children);
            }
        }
    }

    private static void ValidateSyncTree(IEnumerable<PermissionSyncNodeDto> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.Type == PermissionType.Business)
            {
                throw new BusinessException("ClientMenuSyncDoesNotSupportBusinessPermission", StatusCodes.Status400BadRequest);
            }

            ValidateSyncTree(node.Children);
        }
    }

    private Task CreateClientPermissionNodeAsync(
        Guid clientId,
        PermissionSyncNodeDto node,
        Permission? parent,
        List<Permission> createdPermissions,
        List<ClientPermission> createdRelations)
    {
        var permission = new Permission
        {
            Code = node.Code.Trim(),
            Name = node.Name.Trim(),
            DisplayName = node.DisplayName?.Trim(),
            Description = node.Description?.Trim(),
            Type = node.Type,
            Parent = parent,
            Namespace = node.Namespace?.Trim(),
            Resource = node.Resource?.Trim(),
            Action = node.Action?.Trim(),
            Path = node.Path?.Trim(),
            Icon = node.Icon?.Trim(),
            Sort = node.Sort,
            OwnedClientId = clientId,
            TenantId = GetTenantId(),
        };

        createdPermissions.Add(permission);
        createdRelations.Add(new ClientPermission
        {
            ClientId = clientId,
            Permission = permission,
            TenantId = GetTenantId(),
        });

        foreach (var child in node.Children.OrderBy(item => item.Sort).ThenBy(item => item.Code))
        {
            CreateClientPermissionNodeAsync(clientId, child, permission, createdPermissions, createdRelations);
        }

        return Task.CompletedTask;
    }

    private Guid? GetTenantId()
    {
        return _userContext.TenantId == Guid.Empty ? null : _userContext.TenantId;
    }
}