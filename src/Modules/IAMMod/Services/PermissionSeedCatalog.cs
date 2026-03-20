using Entity.IAMMod;

namespace IAMMod.Services;

/// <summary>
/// Default permission seeds for the admin console.
/// </summary>
public static class PermissionSeedCatalog
{
    public const string AdminWebClientCode = "AdminWebClient";

    public static IReadOnlyList<PermissionSeedDefinition> AdminWebMenuPermissions { get; } =
    [
        new("identity", "Identity", "menu.identity", PermissionType.Menu, icon: "people", sort: 0, children:
        [
            new("user", "User", "menu.user", PermissionType.Menu, path: "/user", icon: "manage_accounts", sort: 0),
            new("role", "Role", "menu.role", PermissionType.Menu, path: "/role", icon: "groups", sort: 1),
            new("organization", "Organization", "menu.organization", PermissionType.Menu, path: "/organization", icon: "corporate_fare", sort: 2),
            new("permission", "Permission", "menu.permission", PermissionType.Menu, path: "/permission", icon: "key", sort: 3),
            new("security-audit-logs", "Audit Logs", "menu.auditLogs", PermissionType.Menu, path: "/security/audit-logs", icon: "history", sort: 4),
        ]),
        new("oauth", "OAuth", "menu.oauth", PermissionType.Menu, icon: "security", sort: 1, children:
        [
            new("client", "Client", "menu.application", PermissionType.Menu, path: "/client", icon: "apps", sort: 0),
            new("resource", "Resource", "menu.resource", PermissionType.Menu, path: "/resource", icon: "api", sort: 1),
            new("scope", "Scope", "menu.scope", PermissionType.Menu, path: "/scope", icon: "vpn_key", sort: 2),
            new("security-sessions", "Sessions", "menu.sessions", PermissionType.Menu, path: "/security/sessions", icon: "devices", sort: 3),
        ]),
    ];

    public static IReadOnlyList<PermissionSeedDefinition> DefaultBusinessPermissions { get; } =
    [
        .. BuildCrudSeeds("users", "Users", ["read", "create", "update", "delete", "manage"]),
        .. BuildCrudSeeds("roles", "Roles", ["read", "create", "update", "delete", "assign"]),
        .. BuildCrudSeeds("organizations", "Organizations", ["read", "create", "update", "delete", "manage-members"]),
        .. BuildCrudSeeds("clients", "Clients", ["read", "create", "update", "delete", "manage-secrets"]),
        .. BuildCrudSeeds("resources", "Resources", ["read", "create", "update", "delete"]),
        .. BuildCrudSeeds("scopes", "Scopes", ["read", "create", "update", "delete"]),
        .. BuildCrudSeeds("sessions", "Sessions", ["read", "revoke", "manage"]),
        .. BuildCrudSeeds("audit", "Audit", ["read", "export"]),
        .. BuildCrudSeeds("permissions", "Permissions", ["read", "create", "update", "delete", "assign", "sync-client"]),
    ];

    private static IEnumerable<PermissionSeedDefinition> BuildCrudSeeds(
        string resource,
        string displayName,
        IEnumerable<string> actions)
    {
        foreach (var action in actions)
        {
            yield return new PermissionSeedDefinition(
                Code: $"{resource}.{action}",
                Name: $"{displayName}.{action}",
                DisplayName: $"{displayName} {action}",
                Type: PermissionType.Business,
                @namespace: "iam",
                resource: resource,
                action: action);
        }
    }
}

/// <summary>
/// Permission seed record.
/// </summary>
public sealed record PermissionSeedDefinition(
    string Code,
    string Name,
    string? DisplayName,
    PermissionType Type,
    string? @namespace = null,
    string? resource = null,
    string? action = null,
    string? path = null,
    string? icon = null,
    int sort = 0,
    IReadOnlyList<PermissionSeedDefinition>? children = null)
{
    public string? Namespace { get; init; } = @namespace;
    public string? Resource { get; init; } = resource;
    public string? Action { get; init; } = action;
    public string? Path { get; init; } = path;
    public string? Icon { get; init; } = icon;
    public int Sort { get; init; } = sort;
    public IReadOnlyList<PermissionSeedDefinition> Children { get; init; } = children ?? [];
}