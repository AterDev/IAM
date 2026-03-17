namespace Share.Constants;

public static class PermissionsConst
{
    public const string ClaimType = "permissions";

    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";
    public const string UsersManage = "users.manage";

    public const string RolesRead = "roles.read";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";
    public const string RolesAssign = "roles.assign";

    public const string OrganizationsRead = "organizations.read";
    public const string OrganizationsCreate = "organizations.create";
    public const string OrganizationsUpdate = "organizations.update";
    public const string OrganizationsDelete = "organizations.delete";
    public const string OrganizationsManageMembers = "organizations.manage-members";

    public const string ClientsRead = "clients.read";
    public const string ClientsCreate = "clients.create";
    public const string ClientsUpdate = "clients.update";
    public const string ClientsDelete = "clients.delete";
    public const string ClientsManageSecrets = "clients.manage-secrets";

    public const string ResourcesRead = "resources.read";
    public const string ResourcesCreate = "resources.create";
    public const string ResourcesUpdate = "resources.update";
    public const string ResourcesDelete = "resources.delete";

    public const string ScopesRead = "scopes.read";
    public const string ScopesCreate = "scopes.create";
    public const string ScopesUpdate = "scopes.update";
    public const string ScopesDelete = "scopes.delete";

    public const string SessionsRead = "sessions.read";
    public const string SessionsRevoke = "sessions.revoke";
    public const string SessionsManage = "sessions.manage";

    public const string AuditRead = "audit.read";
    public const string AuditExport = "audit.export";

    public static readonly string[] All =
    [
        UsersRead,
        UsersCreate,
        UsersUpdate,
        UsersDelete,
        UsersManage,
        RolesRead,
        RolesCreate,
        RolesUpdate,
        RolesDelete,
        RolesAssign,
        OrganizationsRead,
        OrganizationsCreate,
        OrganizationsUpdate,
        OrganizationsDelete,
        OrganizationsManageMembers,
        ClientsRead,
        ClientsCreate,
        ClientsUpdate,
        ClientsDelete,
        ClientsManageSecrets,
        ResourcesRead,
        ResourcesCreate,
        ResourcesUpdate,
        ResourcesDelete,
        ScopesRead,
        ScopesCreate,
        ScopesUpdate,
        ScopesDelete,
        SessionsRead,
        SessionsRevoke,
        SessionsManage,
        AuditRead,
        AuditExport,
    ];
}