using ApiService.Models;
using IAMMod.Managers;
using IAMMod.Models.AuditLogDtos;
using IAMMod.Models.AuthorizationDtos;
using IAMMod.Models.LoginSessionDtos;
using IAMMod.Models.OrganizationDtos;
using IAMMod.Models.PermissionDtos;
using IAMMod.Models.ResourceDtos;
using IAMMod.Models.RoleDtos;
using IAMMod.Models.ScopeDtos;
using IAMMod.Models.UserDtos;
using Microsoft.AspNetCore.Mvc;
using UserCenterMod.Managers;
using UserCenterMod.Models;

namespace ApiService.Endpoints;

public sealed class AuditTrailEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/AuditTrail", "AuditTrail", WebConst.AdminUser);
        group.MapGet("", GetPage).WithName("AuditTrail_GetAuditLogs");
        group.MapGet("/{id:guid}", GetDetail).WithName("AuditTrail_GetDetail");
    }

    private static Task<PageList<AuditLogItemDto>> GetPage([AsParameters] AuditLogFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, AuditLogManager manager) => manager.GetPageAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));

    private static async Task<ApiResult<AuditLogDetailDto>> GetDetail(Guid id, AuditLogManager manager)
    {
        var result = await manager.GetDetailAsync(id);
        return result is null ? NotFound("Audit log not found") : result;
    }
}

public sealed class AuthorizationEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/Authorization", "Authorization").RequireAuthorization();
        group.MapGet("", GetAll).WithName("Authorization_GetUserAuthorizations");
        group.MapDelete("/{id:guid}", Revoke).WithName("Authorization_RevokeAuthorization");
        group.MapDelete("/client/{clientId:guid}", RevokeClient).WithName("Authorization_RevokeClientAuthorizations");
    }

    private static async Task<ApiResult<List<UserAuthorizationDto>>> GetAll(HttpContext context, ConsentManager manager)
    {
        var userId = UserId(context);
        if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Unauthorized();
        var authorizations = await manager.GetUserConsentsAsync(userId);
        return authorizations.Select(a => new UserAuthorizationDto
        {
            Id = a.Id,
            ClientId = a.Client?.ClientId ?? string.Empty,
            ClientName = a.Client?.DisplayName ?? a.Client?.ClientId ?? "Unknown",
            Scopes = a.Scopes ?? string.Empty,
            Type = a.Type ?? string.Empty,
            Status = a.Status ?? string.Empty,
            CreationDate = a.CreationDate,
            ExpirationDate = a.ExpirationDate,
        }).ToList();
    }

    private static async Task<ApiResult<ApiMessageDto>> Revoke(Guid id, HttpContext context, ConsentManager manager)
    {
        if (id == Guid.Empty) return BadRequest("Invalid authorization ID");
        var userId = UserId(context);
        if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Unauthorized();
        return await manager.RevokeAuthorizationAsync(userId, id)
            ? new ApiMessageDto("Authorization revoked successfully")
            : NotFound("Authorization not found or already revoked");
    }

    private static async Task<ApiResult<ApiMessageDto>> RevokeClient(Guid clientId, HttpContext context, ConsentManager manager)
    {
        if (clientId == Guid.Empty) return BadRequest("Invalid client ID");
        var userId = UserId(context);
        if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Unauthorized();
        return await manager.RevokeConsentAsync(userId, clientId)
            ? new ApiMessageDto("All authorizations for this client revoked successfully")
            : NotFound("No authorizations found for this client");
    }

    private static string? UserId(HttpContext context) => context.User.FindFirst("sub")?.Value
        ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
}

public sealed class OrganizationsEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/Organizations", "Organizations", WebConst.AdminUser);
        group.MapGet("", GetPage).WithName("Organizations_GetOrganizations");
        group.MapGet("/tree", GetTree).WithName("Organizations_GetTree");
        group.MapGet("/{id:guid}", GetDetail).WithName("Organizations_GetDetail");
        group.MapPost("", Create).WithName("Organizations_CreateOrganization");
        group.MapPut("/{id:guid}", Update).WithName("Organizations_UpdateOrganization");
        group.MapDelete("/{id:guid}", Delete).WithName("Organizations_DeleteOrganization");
        group.MapPost("/{id:guid}/users", AddUsers).WithName("Organizations_AddUsers");
        group.MapDelete("/{id:guid}/users", RemoveUsers).WithName("Organizations_RemoveUsers");
    }

    private static Task<PageList<OrganizationItemDto>> GetPage([AsParameters] OrganizationFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, OrganizationManager manager) => manager.GetPageAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));
    private static Task<List<OrganizationTreeDto>> GetTree(Guid? parentId, OrganizationManager manager) => manager.GetTreeAsync(parentId);
    private static async Task<ApiResult<OrganizationDetailDto>> GetDetail(Guid id, OrganizationManager manager) => (await manager.GetDetailAsync(id)) is { } x ? x : NotFound("Organization not found");
    private static async Task<ApiResult<OrganizationDetailDto>> Create(OrganizationAddDto dto, OrganizationManager manager) => (await manager.AddAsync(dto)) is { } x ? TypedResults.Created($"/api/Organizations/{x.Id}", x) : Problem("Failed to create organization", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<OrganizationDetailDto>> Update(Guid id, OrganizationUpdateDto dto, OrganizationManager manager) => (await manager.UpdateAsync(id, dto)) is { } x ? x : Problem("Failed to update organization", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> Delete(Guid id, bool hardDelete, OrganizationManager manager) => await manager.DeleteAsync(id, !hardDelete) ? TypedResults.NoContent() : Problem("Failed to delete organization", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> AddUsers(Guid id, List<Guid> userIds, OrganizationManager manager) => await manager.AddUsersAsync(id, userIds) ? TypedResults.NoContent() : Problem("Failed to add users to organization", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> RemoveUsers(Guid id, List<Guid> userIds, OrganizationManager manager) => await manager.RemoveUsersAsync(id, userIds) ? TypedResults.NoContent() : Problem("Failed to remove users from organization", StatusCodes.Status400BadRequest);
}

public sealed class PermissionsEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/Permissions", "Permissions", WebConst.AdminUser);
        group.MapGet("", GetPage).WithName("Permissions_GetPermissions");
        group.MapGet("/tree", GetTree).WithName("Permissions_GetPermissionTree");
        group.MapGet("/user-permissions", GetMine).WithName("Permissions_GetUserPermissions");
        group.MapGet("/{id:guid}", GetDetail).WithName("Permissions_GetDetail");
        group.MapPost("", Create).WithName("Permissions_Create");
        group.MapPut("/{id:guid}", Update).WithName("Permissions_Update");
        group.MapDelete("/{id:guid}", Delete).WithName("Permissions_Delete");
    }

    private static Task<PageList<PermissionItemDto>> GetPage([AsParameters] PermissionFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, PermissionManager manager) => manager.GetPageAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));
    private static Task<List<PermissionTreeNodeDto>> GetTree([AsParameters] PermissionFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, PermissionManager manager) => manager.GetTreeAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));
    private static async Task<ApiResult<List<UserPermissionDto>>> GetMine(PermissionManager manager, IUserContext user) => user.UserId == Guid.Empty ? TypedResults.Unauthorized() : await manager.GetCurrentUserPermissionsAsync(user.UserId);
    private static async Task<ApiResult<PermissionDetailDto>> GetDetail(Guid id, PermissionManager manager) => (await manager.GetDetailAsync(id)) is { } x ? x : NotFound("Permission not found");
    private static async Task<ApiResult<PermissionDetailDto>> Create(PermissionUpsertDto dto, PermissionManager manager) => (await manager.AddAsync(dto)) is { } x ? TypedResults.Created($"/api/Permissions/{x.Id}", x) : Problem("Failed to create permission", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<PermissionDetailDto>> Update(Guid id, PermissionUpsertDto dto, PermissionManager manager) => (await manager.UpdateAsync(id, dto)) is { } x ? x : Problem("Failed to update permission", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> Delete(Guid id, PermissionManager manager) => await manager.DeleteAsync(id) ? TypedResults.NoContent() : Problem("Failed to delete permission", StatusCodes.Status400BadRequest);
}

public sealed class ResourcesEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/Resources", "Resources", WebConst.AdminUser);
        group.MapGet("/all", GetAll).WithName("Resources_GetAll");
        group.MapGet("", GetPage).WithName("Resources_GetResources");
        group.MapGet("/{id:guid}", GetDetail).WithName("Resources_GetDetail");
        group.MapPost("", Create).WithName("Resources_CreateResource");
        group.MapPut("/{id:guid}", Update).WithName("Resources_UpdateResource");
        group.MapDelete("/{id:guid}", Delete).WithName("Resources_DeleteResource");
    }

    private static Task<List<ResourceItemDto>> GetAll(ResourceManager manager) => manager.GetAllAsync();
    private static Task<PageList<ResourceItemDto>> GetPage([AsParameters] ResourceFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, ResourceManager manager) => manager.GetPageAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));
    private static async Task<ApiResult<ResourceDetailDto>> GetDetail(Guid id, ResourceManager manager) => (await manager.GetDetailAsync(id)) is { } x ? x : NotFound("Resource not found");
    private static async Task<ApiResult<ResourceDetailDto>> Create(ResourceAddDto dto, ResourceManager manager) => (await manager.AddAsync(dto)) is { } x ? TypedResults.Created($"/api/Resources/{x.Id}", x) : Problem("Failed to create resource", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<ResourceDetailDto>> Update(Guid id, ResourceUpdateDto dto, ResourceManager manager) => (await manager.UpdateAsync(id, dto)) is { } x ? x : Problem("Failed to update resource", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> Delete(Guid id, ResourceManager manager) => await manager.DeleteAsync(id) ? TypedResults.NoContent() : Problem("Failed to delete resource", StatusCodes.Status400BadRequest);
}

public sealed class RolesEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/Roles", "Roles", WebConst.AdminUser);
        group.MapGet("", GetPage).WithName("Roles_GetRoles");
        group.MapGet("/all", GetAll).WithName("Roles_GetAllRoles");
        group.MapGet("/name/{name}", GetByName).WithName("Roles_GetByName");
        group.MapGet("/{id:guid}", GetDetail).WithName("Roles_GetDetail");
        group.MapPost("", Create).WithName("Roles_CreateRole");
        group.MapPut("/{id:guid}", Update).WithName("Roles_UpdateRole");
        group.MapDelete("/{id:guid}", Delete).WithName("Roles_DeleteRole");
        group.MapPost("/{id:guid}/permissions", GrantPermissions).WithName("Roles_GrantPermissions");
        group.MapGet("/{id:guid}/permissions", GetPermissions).WithName("Roles_GetPermissions");
        group.MapGet("/{id:guid}/permission-tree", GetPermissionTree).WithName("Roles_GetPermissionTree");
    }

    private static Task<PageList<RoleItemDto>> GetPage([AsParameters] RoleFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, RoleManager manager) => manager.GetPageAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));
    private static Task<List<RoleItemDto>> GetAll(RoleManager manager) => manager.GetAllAsync();
    private static async Task<ApiResult<RoleDetailDto>> GetDetail(Guid id, RoleManager manager) => (await manager.GetDetailAsync(id)) is { } x ? x : NotFound("Role not found");
    private static async Task<ApiResult<RoleDetailDto>> GetByName(string name, RoleManager manager) => (await manager.GetByNameAsync(name)) is { } x ? x : NotFound("Role not found");
    private static async Task<ApiResult<RoleDetailDto>> Create(RoleAddDto dto, RoleManager manager) => (await manager.AddAsync(dto)) is { } x ? TypedResults.Created($"/api/Roles/{x.Id}", x) : Problem("Failed to create role", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<RoleDetailDto>> Update(Guid id, RoleUpdateDto dto, RoleManager manager) => (await manager.UpdateAsync(id, dto)) is { } x ? x : Problem("Failed to update role", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> Delete(Guid id, bool hardDelete, RoleManager manager) => await manager.DeleteAsync(id, !hardDelete) ? TypedResults.NoContent() : Problem("Failed to delete role", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> GrantPermissions(Guid id, RoleGrantPermissionDto dto, PermissionManager manager, HttpContext context) => await manager.GrantRolePermissionsAsync(id, dto, context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString()) ? TypedResults.Ok() : Problem("Failed to grant permissions to role", StatusCodes.Status400BadRequest);
    private static Task<List<string>> GetPermissions(Guid id, PermissionManager manager) => manager.GetRolePermissionCodesAsync(id);
    private static Task<List<PermissionTreeNodeDto>> GetPermissionTree(Guid id, [AsParameters] PermissionFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, PermissionManager manager) => manager.GetRolePermissionTreeAsync(id, FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));
}

public sealed class ScopesEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/Scopes", "Scopes", WebConst.AdminUser);
        group.MapGet("", GetPage).WithName("Scopes_GetScopes");
        group.MapGet("/{id:guid}", GetDetail).WithName("Scopes_GetDetail");
        group.MapPost("", Create).WithName("Scopes_CreateScope");
        group.MapPut("/{id:guid}", Update).WithName("Scopes_UpdateScope");
        group.MapDelete("/{id:guid}", Delete).WithName("Scopes_DeleteScope");
    }
    private static Task<PageList<ScopeItemDto>> GetPage([AsParameters] ScopeFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, ScopeManager manager) => manager.GetPageAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));
    private static async Task<ApiResult<ScopeDetailDto>> GetDetail(Guid id, ScopeManager manager) => (await manager.GetDetailAsync(id)) is { } x ? x : NotFound("Scope not found");
    private static async Task<ApiResult<ScopeDetailDto>> Create(ScopeAddDto dto, ScopeManager manager) => (await manager.AddAsync(dto)) is { } x ? TypedResults.Created($"/api/Scopes/{x.Id}", x) : Problem("Failed to create scope", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<ScopeDetailDto>> Update(Guid id, ScopeUpdateDto dto, ScopeManager manager) => (await manager.UpdateAsync(id, dto)) is { } x ? x : Problem("Failed to update scope", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> Delete(Guid id, ScopeManager manager) => await manager.DeleteAsync(id) ? TypedResults.NoContent() : Problem("Failed to delete scope", StatusCodes.Status400BadRequest);
}

public sealed class SecurityEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/Security", "Security", WebConst.AdminUser);
        group.MapGet("/sessions", GetSessions).WithName("Security_GetSessions");
        group.MapGet("/sessions/{id:guid}", GetSession).WithName("Security_GetSessionDetail");
        group.MapPost("/sessions/{id:guid}/revoke", Revoke).WithName("Security_RevokeSession");
        group.MapPost("/sessions/revoke-all", RevokeAll).WithName("Security_RevokeAllSessions");
        group.MapPost("/logs", GetLogs).WithName("Security_GetAuditLogs");
        group.MapGet("/logs/{id:guid}", GetLog).WithName("Security_GetAuditLogDetail");
    }
    private static Task<PageList<LoginSessionItemDto>> GetSessions([AsParameters] LoginSessionFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, SessionManager manager) => manager.GetPageAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));
    private static async Task<ApiResult<LoginSessionDetailDto>> GetSession(Guid id, SessionManager manager) => (await manager.GetDetailAsync(id)) is { } x ? x : NotFound("Login session not found");
    private static async Task<ApiResult<ApiMessageDto>> Revoke(Guid id, SessionManager manager, IUserContext user, HttpContext context) => await manager.RevokeSessionAsync(id, user.UserId.ToString(), context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString()) ? new ApiMessageDto("Session revoked successfully") : NotFound("Login session not found or already revoked");
    private static async Task<ApiResult<ApiMessageDto>> RevokeAll(bool exceptCurrent, SessionManager manager, IUserContext user, HttpContext context)
    {
        if (user.UserId == Guid.Empty) return TypedResults.Unauthorized();
        var currentSessionId = exceptCurrent ? context.User.FindFirst("sid")?.Value : null;
        var count = await manager.RevokeAllUserSessionsAsync(user.UserId, currentSessionId, user.UserId.ToString(), context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString());
        return new ApiMessageDto($"Revoked {count} session(s)") { Count = count };
    }
    private static Task<PageList<AuditLogItemDto>> GetLogs([FromBody] AuditLogFilterDto filter, AuditLogManager manager) => manager.GetPageAsync(filter);
    private static async Task<ApiResult<AuditLogDetailDto>> GetLog(Guid id, AuditLogManager manager) => (await manager.GetDetailAsync(id)) is { } x ? x : NotFound("Audit log not found");
}

public sealed class UsersEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/Users", "Users", WebConst.AdminUser);
        group.MapGet("", GetPage).WithName("Users_GetUsers");
        group.MapGet("/username/{username}", GetByName).WithName("Users_GetByUserName");
        group.MapGet("/{id:guid}", GetDetail).WithName("Users_GetDetail");
        group.MapPost("", Create).WithName("Users_CreateUser");
        group.MapPut("/{id:guid}", Update).WithName("Users_UpdateUser");
        group.MapPatch("/{id:guid}/status", UpdateStatus).WithName("Users_UpdateStatus");
        group.MapDelete("/{id:guid}", Delete).WithName("Users_DeleteUser");
        group.MapPost("/{id:guid}/password", ChangePassword).WithName("Users_ChangePassword");
        group.MapPost("/{id:guid}/roles", AssignRoles).WithName("Users_AssignRoles");
    }
    private static Task<PageList<UserItemDto>> GetPage([AsParameters] UserFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, UserManager manager) => manager.GetPageAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));
    private static async Task<ApiResult<UserDetailDto>> GetDetail(Guid id, UserManager manager) => (await manager.GetDetailAsync(id)) is { } x ? x : NotFound("User not found");
    private static async Task<ApiResult<UserDetailDto>> GetByName(string username, UserManager manager) => (await manager.GetByUserNameAsync(username)) is { } x ? x : NotFound("User not found");
    private static async Task<ApiResult<UserDetailDto>> Create(UserAddDto dto, UserManager manager) => (await manager.AddAsync(dto)) is { } x ? TypedResults.Created($"/api/Users/{x.Id}", x) : Problem("Failed to create user", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<UserDetailDto>> Update(Guid id, UserUpdateDto dto, UserManager manager) => (await manager.UpdateAsync(id, dto)) is { } x ? x : Problem("Failed to update user", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> UpdateStatus(Guid id, [FromBody] DateTimeOffset? lockoutEnd, UserManager manager) => await manager.SetLockoutAsync(id, lockoutEnd) ? TypedResults.NoContent() : Problem("Failed to update user status", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> Delete(Guid id, bool hardDelete, UserManager manager) => await manager.DeleteAsync(id, !hardDelete) ? TypedResults.NoContent() : Problem("Failed to delete user", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> ChangePassword(Guid id, [FromBody] string newPassword, UserManager manager) => string.IsNullOrWhiteSpace(newPassword) ? BadRequest("Password cannot be empty") : await manager.ChangePasswordAsync(id, newPassword) ? TypedResults.NoContent() : Problem("Failed to change password", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> AssignRoles(Guid id, List<Guid> roleIds, UserManager manager, HttpContext context) => await manager.AssignRolesAsync(id, roleIds, context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString()) ? TypedResults.NoContent() : Problem("Failed to assign roles to user", StatusCodes.Status400BadRequest);
}

public sealed class UserEntitlementDefinitionEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/UserEntitlementDefinitions", "UserEntitlementDefinitions", WebConst.AdminUser);
        group.MapGet("", GetPage).WithName("UserEntitlementDefinitions_GetPage"); group.MapGet("/{id:guid}", GetDetail).WithName("UserEntitlementDefinitions_GetDetail"); group.MapPost("", Create).WithName("UserEntitlementDefinitions_Create"); group.MapPut("/{id:guid}", Update).WithName("UserEntitlementDefinitions_Update"); group.MapDelete("/{id:guid}", Delete).WithName("UserEntitlementDefinitions_Delete");
    }
    private static Task<PageList<UserEntitlementDefinitionItemDto>> GetPage([AsParameters] UserEntitlementDefinitionFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, UserEntitlementDefinitionManager manager, CancellationToken ct) => manager.GetPageAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy), ct);
    private static async Task<ApiResult<UserEntitlementDefinitionItemDto>> GetDetail(Guid id, UserEntitlementDefinitionManager manager, CancellationToken ct) => (await manager.GetDetailAsync(id, ct)) is { } x ? x : NotFound("NotFound");
    private static async Task<ApiResult<UserEntitlementDefinitionItemDto>> Create(UserEntitlementDefinitionUpsertDto dto, UserEntitlementDefinitionManager manager, CancellationToken ct) { var x = await manager.AddAsync(dto, ct); return TypedResults.Created($"/api/UserEntitlementDefinitions/{x.Id}", x); }
    private static Task<UserEntitlementDefinitionItemDto> Update(Guid id, UserEntitlementDefinitionUpsertDto dto, UserEntitlementDefinitionManager manager, CancellationToken ct) => manager.UpdateAsync(id, dto, ct);
    private static async Task<ApiResult<bool>> Delete(Guid id, UserEntitlementDefinitionManager manager, CancellationToken ct) { await manager.DeleteAsync(id, ct); return TypedResults.NoContent(); }
}

public sealed class UserEntitlementsEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/UserEntitlements", "UserEntitlements", WebConst.AdminUser);
        group.MapGet("", GetPage).WithName("UserEntitlements_GetPage"); group.MapGet("/{id:guid}", GetDetail).WithName("UserEntitlements_GetDetail"); group.MapPost("/users/{userId:guid}", Create).WithName("UserEntitlements_Create"); group.MapPut("/{id:guid}", Update).WithName("UserEntitlements_Update"); group.MapDelete("/{id:guid}", Delete).WithName("UserEntitlements_Delete");
    }
    private static Task<PageList<UserEntitlementDetailDto>> GetPage([AsParameters] UserEntitlementFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, UserEntitlementManager manager, CancellationToken ct) => manager.GetPageAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy), ct);
    private static async Task<ApiResult<UserEntitlementDetailDto>> GetDetail(Guid id, UserEntitlementManager manager, CancellationToken ct) => (await manager.GetDetailAsync(id, ct)) is { } x ? x : NotFound("NotFound");
    private static async Task<ApiResult<UserEntitlementDetailDto>> Create(Guid userId, UserEntitlementAddDto dto, UserEntitlementManager manager, CancellationToken ct) { var x = await manager.AddAsync(userId, dto, ct); return TypedResults.Created($"/api/UserEntitlements/{x.Id}", x); }
    private static Task<UserEntitlementDetailDto> Update(Guid id, UserEntitlementUpdateDto dto, UserEntitlementManager manager, CancellationToken ct) => manager.UpdateAsync(id, dto, ct);
    private static async Task<ApiResult<bool>> Delete(Guid id, UserEntitlementManager manager, CancellationToken ct) { await manager.DeleteAsync(id, ct); return TypedResults.NoContent(); }
}
