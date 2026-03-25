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
        new("identity", "Identity", PermissionType.Menu, children:
        [
            new("user", "User", PermissionType.Menu, path: "/user"),
            new("role", "Role", PermissionType.Menu, path: "/role"),
            new("organization", "Organization", PermissionType.Menu, path: "/organization"),
            new("security-audit-logs", "Audit Logs", PermissionType.Menu, path: "/security/audit-logs"),
        ]),
        new("oauth", "OAuth", PermissionType.Menu, children:
        [
            new("client", "Client", PermissionType.Menu, path: "/client"),
            new("resource", "Resource", PermissionType.Menu, path: "/resource"),
            new("scope", "Scope", PermissionType.Menu, path: "/scope"),
            new("security-sessions", "Sessions", PermissionType.Menu, path: "/security/sessions"),
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
                Name: $"{displayName} {action}",
                Type: PermissionType.Business);
        }
    }
}

/// <summary>
/// Permission seed record.
/// </summary>
public sealed record PermissionSeedDefinition(
    string Code,
    string Name,
    PermissionType Type,
    string? path = null,
    IReadOnlyList<PermissionSeedDefinition>? children = null)
{
    public string? Path { get; init; } = path;
    public IReadOnlyList<PermissionSeedDefinition> Children { get; init; } = children ?? [];
}