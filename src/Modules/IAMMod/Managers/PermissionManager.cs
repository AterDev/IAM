using System.Linq.Expressions;
using Perigon.PostgreSQL;
using IAMMod.Models.PermissionDtos;
using IAMMod.Models.RoleDtos;
using Microsoft.AspNetCore.Http;
using Share.Exceptions;
using System.Text.Json.Nodes;

namespace IAMMod.Managers;

/// <summary>
/// Manager for the unified permission model.
/// </summary>
public class PermissionManager(
    AppDbFactory dbContextFactory,
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
        var query = await BuildFilteredPermissionQueryAsync(filter, clientId);

        query = OrderPermissions(query, filter.OrderBy);

        var count = await query.CountAsync();
        var data = await query
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PageList<PermissionItemDto>
        {
            Count = count,
            Data = data.Select(PermissionItemDto.FromEntity).ToList(),
            PageIndex = filter.PageIndex,
        };
    }

    /// <summary>
    /// Get permission detail.
    /// </summary>
    public async Task<PermissionDetailDto?> GetDetailAsync(Guid id)
    {
        var permission = await _dbContext.Permissions
            .Include(permission => permission.Parent)
            .Include(permission => permission.OwnedClient)
            .Where(permission => permission.Id == id)
            .FirstOrDefaultAsync();
        return permission is null ? null : PermissionDetailDto.FromEntity(permission);
    }

    /// <summary>
    /// Get permission tree.
    /// </summary>
    public async Task<List<PermissionTreeNodeDto>> GetTreeAsync(PermissionFilterDto filter)
    {
        var clientId = await ResolveClientIdAsync(filter.ClientId, filter.ClientCode);
        var items = await (await BuildFilteredPermissionQueryAsync(filter, clientId))
            .OrderBy(permission => permission.Code)
            .ToListAsync();

        return BuildTree(items.Select(PermissionTreeNodeDto.FromEntity).ToList(), []);
    }

    /// <summary>
    /// Get permission tree with role selection state.
    /// </summary>
    public async Task<List<PermissionTreeNodeDto>> GetRolePermissionTreeAsync(Guid roleId, PermissionFilterDto filter)
    {
        var selectedCodes = await GetRolePermissionCodesAsync(roleId);

        var clientId = await ResolveClientIdAsync(filter.ClientId, filter.ClientCode);
        var items = await (await BuildFilteredPermissionQueryAsync(filter, clientId))
            .OrderBy(permission => permission.Code)
            .ToListAsync();

        return BuildTree(items.Select(PermissionTreeNodeDto.FromEntity).ToList(), selectedCodes);
    }

    /// <summary>
    /// Get permission tree with client selection state.
    /// </summary>
    public async Task<List<PermissionTreeNodeDto>> GetClientPermissionTreeAsync(Guid clientId, PermissionFilterDto filter)
    {
        filter.ClientId = clientId;

        var selectedCodes = await GetClientPermissionCodesAsync(clientId);

        var items = await (await BuildFilteredPermissionQueryAsync(filter, clientId))
            .OrderBy(permission => permission.Code)
            .ToListAsync();

        return BuildTree(items.Select(PermissionTreeNodeDto.FromEntity).ToList(), selectedCodes);
    }

    /// <summary>
    /// Get the current user's effective permissions from all assigned roles.
    /// </summary>
    public async Task<List<UserPermissionDto>> GetCurrentUserPermissionsAsync(Guid userId)
    {
        var roleIds = await _dbContext.UserRoles
            .Where(item => item.UserId == userId)
            .Select(item => item.RoleId)
            .Distinct()
            .ToScalarListAsync();

        if (roleIds.Count == 0)
        {
            return [];
        }

        var permissionIds = await _dbContext.RolePermissions
            .Where(item => Enumerable.Contains(roleIds, item.RoleId))
            .Select(item => item.PermissionId).Distinct().ToScalarListAsync();
        var items = await _dbContext.Permissions.Include(item => item.OwnedClient)
            .Where(item => Enumerable.Contains(permissionIds, item.Id)).ToListAsync();
        return items.Select(item => new UserPermissionDto
        {
            Code = item.Code,
            Type = item.Type,
            OwnedClientCode = item.OwnedClient?.ClientId,
        }).OrderBy(item => item.OwnedClientCode).ThenBy(item => item.Type).ThenBy(item => item.Code).ToList();
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

        await _dbContext.TransactionAsync(async ct =>
        {
            await _dbContext.Permissions.InsertAsync(entity, ct);
            if (dto.OwnedClientId.HasValue)
            {
                await EnsureClientPermissionAsync(dto.OwnedClientId.Value, entity);
            }
        });
        return await GetDetailAsync(entity.Id);
    }

    /// <summary>
    /// Update a permission.
    /// </summary>
    public async Task<PermissionDetailDto?> UpdateAsync(Guid id, PermissionUpsertDto dto)
    {
        var entity = await _dbContext.Permissions
            .Where(permission => permission.Id == id).FirstOrDefaultAsync();

        if (entity == null)
        {
            throw new BusinessException("PermissionNotFound", StatusCodes.Status404NotFound);
        }

        await ValidatePermissionUpsertAsync(dto, id);

        var oldOwnedClientId = entity.OwnedClientId;
        ApplyPermissionValues(entity, dto);
        entity.UpdatedTime = DateTimeOffset.UtcNow;

        await _dbContext.TransactionAsync(async ct =>
        {
            await _dbContext.Permissions.Where(item => item.Id == id).ExecuteUpdateAsync(update => update
                .Set(item => item.Code, entity.Code)
                .Set(item => item.Name, entity.Name)
                .Set(item => item.Type, entity.Type)
                .Set(item => item.Description, entity.Description)
                .Set(item => item.ParentId, entity.ParentId)
                .Set(item => item.Path, entity.Path)
                .Set(item => item.OwnedClientId, entity.OwnedClientId)
                .Set(item => item.UpdatedTime, entity.UpdatedTime), cancellationToken: ct);

            if (oldOwnedClientId != dto.OwnedClientId)
            {
                if (oldOwnedClientId.HasValue)
                {
                    await _dbContext.ClientPermissions
                        .Where(item => item.ClientId == oldOwnedClientId && item.PermissionId == entity.Id)
                        .ExecuteDeleteAsync();
                }

                if (dto.OwnedClientId.HasValue)
                {
                    await EnsureClientPermissionAsync(dto.OwnedClientId.Value, entity);
                }
            }
        });
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
            .Where(item => Enumerable.Contains(permissionIds, item.PermissionId))
            .ExecuteDeleteAsync();

        await _dbContext.ClientPermissions
            .Where(item => Enumerable.Contains(permissionIds, item.PermissionId))
            .ExecuteDeleteAsync();

        await _dbContext.Permissions
            .Where(item => Enumerable.Contains(permissionIds, item.Id))
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
        var role = await _dbContext.Roles.Where(item => item.Id == roleId).FirstOrDefaultAsync();
        if (role == null)
        {
            throw new BusinessException("RoleNotFound", StatusCodes.Status404NotFound);
        }

        var permissionCodes = NormalizePermissionCodes(dto.PermissionCodes);
        var permissions = permissionCodes.Count == 0
            ? []
            : await _dbContext.Permissions
                .Where(item => Enumerable.Contains(permissionCodes, item.Code))
                .ToListAsync();

        if (permissions.Count != permissionCodes.Count)
        {
            throw new BusinessException("PermissionNotFound", StatusCodes.Status404NotFound);
        }

        await _dbContext.TransactionAsync(async ct =>
        {
            await _dbContext.RolePermissions.Where(item => item.RoleId == roleId).ExecuteDeleteAsync();

            if (permissions.Count > 0)
            {
                var entities = permissions.Select(item => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = item.Id,
                }).ToList();

                await _dbContext.RolePermissions.BulkInsertAsync(entities.ToArray());
            }
        });

        await _auditLogManager.AddAuditLogAsync(
            category: "Authorization",
            eventName: "RolePermissionsChanged",
            subjectId: roleId.ToString(),
            payload: new JsonObject
            {
                ["Name"] = role.Name,
                ["count"] = permissionCodes.Count,
                ["permissionCodes"] = new JsonArray(permissionCodes.Select(code => (JsonNode?)JsonValue.Create(code)).ToArray()),
            }.ToJsonString(),
            ipAddress: ipAddress,
            userAgent: userAgent);

        return true;
    }

    /// <summary>
    /// Get role permission codes.
    /// </summary>
    public async Task<List<string>> GetRolePermissionCodesAsync(Guid roleId)
    {
        var permissionIds = await _dbContext.RolePermissions.Where(item => item.RoleId == roleId)
            .Select(item => item.PermissionId).ToScalarListAsync();
        return await _dbContext.Permissions.Where(item => Enumerable.Contains(permissionIds, item.Id))
            .OrderBy(item => item.Code).Select(item => item.Code).ToScalarListAsync();
    }

    /// <summary>
    /// Replace client permissions.
    /// </summary>
    public async Task<bool> AssignClientPermissionsAsync(Guid clientId, IEnumerable<string>? permissionCodes)
    {
        var client = await _dbContext.Clients.Where(item => item.Id == clientId).FirstOrDefaultAsync();
        if (client == null)
        {
            throw new BusinessException(Localizer.ClientNotFound, StatusCodes.Status404NotFound);
        }

        var normalizedPermissionCodes = NormalizePermissionCodes(permissionCodes);
        var permissions = normalizedPermissionCodes.Count == 0
            ? []
            : await _dbContext.Permissions
                .Where(item => Enumerable.Contains(normalizedPermissionCodes, item.Code))
                .ToListAsync();

        if (permissions.Count != normalizedPermissionCodes.Count)
        {
            throw new BusinessException("PermissionNotFound", StatusCodes.Status404NotFound);
        }

        await _dbContext.TransactionAsync(async ct =>
        {
            await _dbContext.ClientPermissions.Where(item => item.ClientId == clientId).ExecuteDeleteAsync();

            if (permissions.Count > 0)
            {
                var entities = permissions.Select(item => new ClientPermission
                {
                    ClientId = clientId,
                    PermissionId = item.Id,
                }).ToList();

                await _dbContext.ClientPermissions.BulkInsertAsync(entities.ToArray());
            }
        });
        return true;
    }

    /// <summary>
    /// Get client permission codes.
    /// </summary>
    public async Task<List<string>> GetClientPermissionCodesAsync(Guid clientId)
    {
        var permissionIds = await _dbContext.ClientPermissions.Where(item => item.ClientId == clientId)
            .Select(item => item.PermissionId).ToScalarListAsync();
        return await _dbContext.Permissions.Where(item => Enumerable.Contains(permissionIds, item.Id))
            .OrderBy(item => item.Code).Select(item => item.Code).ToScalarListAsync();
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
        var client = await _dbContext.Clients.Where(item => item.Id == clientId).FirstOrDefaultAsync();
        if (client == null)
        {
            throw new BusinessException(Localizer.ClientNotFound, StatusCodes.Status404NotFound);
        }

        ValidateSyncTree(dto.Permissions);

        var createdPermissions = new List<Permission>();
        await _dbContext.TransactionAsync(async ct =>
        {
            var ownedPermissionIds = await _dbContext.Permissions
                .Where(item => item.OwnedClientId == clientId)
                .Where(item => item.Type == PermissionType.Menu || item.Type == PermissionType.Button)
                .Select(item => item.Id)
                .ToScalarListAsync();

            var linkedIds = await _dbContext.ClientPermissions.Where(item => item.ClientId == clientId)
                .Select(item => item.PermissionId).ToScalarListAsync();
            var linkedPermissionIds = await _dbContext.Permissions.Where(item => Enumerable.Contains(linkedIds, item.Id))
                .Where(item => item.Type == PermissionType.Menu || item.Type == PermissionType.Button)
                .Select(item => item.Id).ToScalarListAsync();

            var scopedPermissionIds = ownedPermissionIds
                .Concat(linkedPermissionIds)
                .Distinct()
                .ToList();

            if (linkedPermissionIds.Count > 0)
            {
                await _dbContext.ClientPermissions
                    .Where(item => item.ClientId == clientId)
                    .Where(item => Enumerable.Contains(linkedPermissionIds, item.PermissionId))
                    .ExecuteDeleteAsync();
            }

            if (scopedPermissionIds.Count > 0)
            {
                var stillLinkedIds = await _dbContext.ClientPermissions
                    .Where(item => Enumerable.Contains(scopedPermissionIds, item.PermissionId))
                    .Select(item => item.PermissionId).Distinct().ToScalarListAsync();
                var orphanPermissionIds = await _dbContext.Permissions
                    .Where(item => Enumerable.Contains(scopedPermissionIds, item.Id))
                    .Where(item => item.Type == PermissionType.Menu || item.Type == PermissionType.Button)
                    .Where(item => !Enumerable.Contains(stillLinkedIds, item.Id))
                    .Select(item => item.Id)
                    .ToScalarListAsync();

                if (orphanPermissionIds.Count > 0)
                {
                    await _dbContext.RolePermissions
                        .Where(item => Enumerable.Contains(orphanPermissionIds, item.PermissionId))
                        .ExecuteDeleteAsync();

                    var deletablePermissionIds = await _dbContext.Permissions
                        .Where(item => Enumerable.Contains(orphanPermissionIds, item.Id))
                        .Where(item => item.OwnedClientId == clientId)
                        .Select(item => item.Id)
                        .ToScalarListAsync();

                    if (deletablePermissionIds.Count > 0)
                    {
                        await _dbContext.Permissions
                            .Where(item => Enumerable.Contains(deletablePermissionIds, item.Id))
                            .ExecuteDeleteAsync();
                    }
                }
            }

            if (ownedPermissionIds.Count > 0)
            {
                await _dbContext.Permissions
                    .Where(item => Enumerable.Contains(ownedPermissionIds, item.Id))
                    .ExecuteDeleteAsync();
            }


            var createdRelations = new List<ClientPermission>();
            foreach (var node in dto.Permissions.OrderBy(item => item.Code))
            {
                CreateClientPermissionNode(clientId, node, null, createdPermissions, createdRelations);
            }

            if (createdPermissions.Count > 0)
            {
                await _dbContext.Permissions.BulkInsertAsync(createdPermissions.ToArray());
                await _dbContext.ClientPermissions.BulkInsertAsync(createdRelations.ToArray());
            }
        });

        await _auditLogManager.AddAuditLogAsync(
            category: "Authorization",
            eventName: "ClientMenuPermissionsSynced",
            subjectId: clientId.ToString(),
            payload: new JsonObject { ["ClientId"] = client.ClientId, ["count"] = createdPermissions.Count }.ToJsonString(),
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

        var client = await _dbContext.Clients.Where(item => item.ClientIdentifier == clientCode).FirstOrDefaultAsync();
        return client?.Id;
    }

    private async Task<IQueryable<Permission>> BuildFilteredPermissionQueryAsync(PermissionFilterDto filter, Guid? clientId)
    {
        var query = _dbContext.Permissions
            .Include(permission => permission.Parent)
            .Include(permission => permission.OwnedClient);

        query = query
            .WhereNotNull(filter.Type, permission => permission.Type == filter.Type)
            .WhereNotNull(filter.ParentId, permission => permission.ParentId == filter.ParentId)

            .WhereNotNull(
                filter.Keyword,
                permission =>
                    permission.Code.Contains(filter.Keyword!)
                    || permission.Name.Contains(filter.Keyword!)
                    || (permission.Description != null && permission.Description.Contains(filter.Keyword!)));

        if (filter.OnlyNonBusiness == true)
        {
            query = query.Where(permission => permission.Type == PermissionType.Menu || permission.Type == PermissionType.Button);
        }

        if (clientId.HasValue)
        {
            var linkedIds = await _dbContext.ClientPermissions.Where(item => item.ClientId == clientId)
                .Select(item => item.PermissionId).ToScalarListAsync();
            query = query.Where(permission => Enumerable.Contains(linkedIds, permission.Id));
        }

        return query;
    }

    private static IQueryable<Permission> OrderPermissions(IQueryable<Permission> query, Dictionary<string, bool>? orderBy)
    {
        IOrderedQueryable<Permission>? ordered = null;
        foreach (var (field, ascending) in orderBy ?? [])
        {
            ordered = field.ToUpperInvariant() switch
            {
                "ID" => AddOrder(item => item.Id, ascending),
                "CODE" => AddOrder(item => item.Code, ascending),
                "NAME" => AddOrder(item => item.Name, ascending),
                "DESCRIPTION" => AddOrder(item => item.Description, ascending),
                "TYPE" => AddOrder(item => item.Type, ascending),
                "PATH" => AddOrder(item => item.Path, ascending),
                "PARENTID" => AddOrder(item => item.ParentId, ascending),
                "OWNEDCLIENTID" => AddOrder(item => item.OwnedClientId, ascending),
                "CREATEDTIME" => AddOrder(item => item.CreatedTime, ascending),
                "UPDATEDTIME" => AddOrder(item => item.UpdatedTime, ascending),
                _ => ordered,
            };
        }
        return ordered ?? query.OrderBy(item => item.Code);

        IOrderedQueryable<Permission> AddOrder<TKey>(Expression<Func<Permission, TKey>> key, bool ascending) =>
            ordered is null
                ? (ascending ? query.OrderBy(key) : query.OrderByDescending(key))
                : (ascending ? ordered.ThenBy(key) : ordered.ThenByDescending(key));
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
        if (await _dbContext.Permissions.Where(item => item.Code == dto.Code && item.Id != currentId).AnyAsync())
        {
            throw new BusinessException("PermissionCodeExists", StatusCodes.Status400BadRequest);
        }

        if (dto.ParentId.HasValue)
        {
            var parent = await _dbContext.Permissions.Where(item => item.Id == dto.ParentId).FirstOrDefaultAsync();
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
            var clientExists = await _dbContext.Clients.Where(item => item.Id == dto.OwnedClientId).AnyAsync();
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
        entity.Description = dto.Description?.Trim();
        entity.Type = dto.Type;
        entity.ParentId = dto.ParentId;
        entity.Path = dto.Path?.Trim();
        entity.OwnedClientId = dto.OwnedClientId;
    }

    private async Task EnsureClientPermissionAsync(Guid clientId, Permission permission)
    {
        var exists = await _dbContext.ClientPermissions
            .Where(item => item.ClientId == clientId && item.PermissionId == permission.Id).AnyAsync();
        if (!exists)
        {
            await _dbContext.ClientPermissions.InsertAsync(new ClientPermission
            {
                ClientId = clientId,
                PermissionId = permission.Id,
            });
        }
    }
    private async Task<List<Guid>> CollectPermissionIdsAsync(Guid rootId)
    {
        var existingIds = await _dbContext.Permissions
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
        foreach (var node in nodes.OrderBy(item => item.Code))
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
        nodes.Sort((left, right) => string.Compare(left.Code, right.Code, StringComparison.Ordinal));

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

    private static void CreateClientPermissionNode(
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
            Description = node.Description?.Trim(),
            Type = node.Type,
            ParentId = parent?.Id,
            Path = node.Path?.Trim(),
            OwnedClientId = clientId,
        };

        createdPermissions.Add(permission);
        createdRelations.Add(new ClientPermission
        {
            ClientId = clientId,
            PermissionId = permission.Id,
        });

        foreach (var child in node.Children.OrderBy(item => item.Code))
        {
            CreateClientPermissionNode(clientId, child, permission, createdPermissions, createdRelations);
        }

    }

}
