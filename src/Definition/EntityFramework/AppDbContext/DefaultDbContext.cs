namespace EntityFramework.AppDbContext;

/// <summary>
/// default data access for main business
/// </summary>
/// <param name="options"></param>
public partial class DefaultDbContext(DbContextOptions<DefaultDbContext> options)
    : ContextBase(options)
{
    // Common entities
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SigningKey> SigningKeys { get; set; }
    public DbSet<ProxyUsage> ProxyUsages { get; set; }
    public DbSet<UserEntitlementDefinition> UserEntitlementDefinitions { get; set; }
    public DbSet<UserEntitlement> UserEntitlements { get; set; }

    // Identity entities
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserClaim> UserClaims { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserLogin> UserLogins { get; set; }
    public DbSet<UserToken> UserTokens { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationUser> OrganizationUsers { get; set; }
    public DbSet<LoginSession> LoginSessions { get; set; }

    // Access entities
    public DbSet<Client> Clients { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<ClientPermission> ClientPermissions { get; set; }
    public DbSet<ClientSecret> ClientSecrets { get; set; }
    public DbSet<ApiScope> ApiScopes { get; set; }
    public DbSet<ClientScope> ClientScopes { get; set; }
    public DbSet<ScopeClaim> ScopeClaims { get; set; }
    public DbSet<ApiResource> ApiResources { get; set; }
    public DbSet<ClientResource> ClientResources { get; set; }
    public DbSet<Authorization> Authorizations { get; set; }
    public DbSet<Token> Tokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        EnableSoftDeleteFilter<EntityBase>("SoftDelete", entity => !entity.IsDeleted);
        ConfigureCommonEntities(builder);
        ConfigureIdentityEntities(builder);
        ConfigureAccessEntities(builder);
    }

    private void ConfigureCommonEntities(ModelBuilder builder)
    {
        // SigningKey configuration
        builder.Entity<SigningKey>(entity =>
        {
            entity.HasIndex(e => e.KeyId).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // ProxyUsage configuration
        builder.Entity<ProxyUsage>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Date);
        });

        builder.Entity<UserEntitlementDefinition>(entity =>
        {
            entity.HasIndex(e => e.EntitlementCode).IsUnique();
        });

        builder.Entity<UserEntitlement>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EntitlementDefinitionId);
            entity.HasOne(e => e.EntitlementDefinition)
                .WithMany(e => e.UserEntitlements)
                .HasForeignKey(e => e.EntitlementDefinitionId)
                .OnDelete(ReferentialAction.Restrict);
        });
    }

    private void ConfigureIdentityEntities(ModelBuilder builder)
    {
        // User configuration
        builder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.NormalizedUserName);
            entity.HasIndex(e => e.NormalizedEmail).IsUnique();
        });

        // Role configuration
        builder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName).IsUnique();
        });

        // UserRole configuration
        builder.Entity<UserRole>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(ReferentialAction.Cascade);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(ReferentialAction.Cascade);
        });

        // UserClaim configuration
        builder.Entity<UserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserClaims)
                .HasForeignKey(e => e.UserId)
                .OnDelete(ReferentialAction.Cascade);
        });

        // RolePermission configuration
        builder.Entity<RolePermission>(entity =>
        {
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(ReferentialAction.Cascade);
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(ReferentialAction.Cascade);
        });

        // UserLogin configuration
        builder.Entity<UserLogin>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserLogins)
                .HasForeignKey(e => e.UserId)
                .OnDelete(ReferentialAction.Cascade);
        });

        // UserToken configuration
        builder.Entity<UserToken>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(ReferentialAction.Cascade);
        });

        // Organization configuration
        builder.Entity<Organization>(entity =>
        {
            entity.HasIndex(e => e.ParentId);
            entity.HasOne(e => e.Parent)
                .WithMany(o => o.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(ReferentialAction.Restrict);
        });

        // OrganizationUser configuration
        builder.Entity<OrganizationUser>(entity =>
        {
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.OrganizationUsers)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(ReferentialAction.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.OrganizationUsers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(ReferentialAction.Cascade);
        });

        // LoginSession configuration
        builder.Entity<LoginSession>(entity =>
        {
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.LoginSessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(ReferentialAction.Cascade);
        });
    }

    private void ConfigureAccessEntities(ModelBuilder builder)
    {
        // Client configuration
        builder.Entity<Client>(entity =>
        {
            entity.HasIndex(e => e.ClientIdentifier).IsUnique();
            entity.HasIndex(e => e.RegistrationStatus);
            entity.HasIndex(e => e.DeveloperUserId);
        });

        builder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => e.OwnedClientId);
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(ReferentialAction.Restrict);
            entity.HasOne(e => e.OwnedClient)
                .WithMany(c => c.OwnedPermissions)
                .HasForeignKey(e => e.OwnedClientId)
                .OnDelete(ReferentialAction.Restrict);
        });

        builder.Entity<ClientPermission>(entity =>
        {
            entity.HasOne(e => e.Client)
                .WithMany(c => c.ClientPermissions)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(ReferentialAction.Cascade);
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.ClientPermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(ReferentialAction.Cascade);
        });

        builder.Entity<ClientSecret>(entity =>
        {
            entity.HasIndex(e => e.ClientId);
            entity.HasOne(e => e.Client)
                .WithMany(c => c.ClientSecrets)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(ReferentialAction.Cascade);
        });

        // ApiScope configuration
        builder.Entity<ApiScope>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // ClientScope configuration
        builder.Entity<ClientScope>(entity =>
        {
            entity.HasOne(e => e.Client)
                .WithMany(c => c.ClientScopes)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(ReferentialAction.Cascade);
            entity.HasOne(e => e.Scope)
                .WithMany(s => s.ClientScopes)
                .HasForeignKey(e => e.ScopeId)
                .OnDelete(ReferentialAction.Cascade);
        });

        // ScopeClaim configuration
        builder.Entity<ScopeClaim>(entity =>
        {
            entity.HasIndex(e => e.ScopeId);
            entity.HasOne(e => e.Scope)
                .WithMany(s => s.ScopeClaims)
                .HasForeignKey(e => e.ScopeId)
                .OnDelete(ReferentialAction.Cascade);
        });

        // ApiResource configuration
        builder.Entity<ApiResource>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // ClientResource configuration
        builder.Entity<ClientResource>(entity =>
        {
            entity.HasOne(e => e.Client)
                .WithMany(c => c.ClientResources)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(ReferentialAction.Cascade);
            entity.HasOne(e => e.ApiResource)
                .WithMany(r => r.ClientResources)
                .HasForeignKey(e => e.ApiResourceId)
                .OnDelete(ReferentialAction.Cascade);
        });

        // Authorization configuration
        builder.Entity<Authorization>(entity =>
        {
            entity.HasIndex(e => e.SubjectId);
            entity.HasIndex(e => e.ClientId);
            entity.HasOne(e => e.Client)
                .WithMany(c => c.Authorizations)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(ReferentialAction.Cascade);
        });

        // Token configuration
        builder.Entity<Token>(entity =>
        {
            entity.HasIndex(e => e.ReferenceId);
            entity.HasIndex(e => e.AuthorizationId);
            entity.HasOne(e => e.Authorization)
                .WithMany(a => a.Tokens)
                .HasForeignKey(e => e.AuthorizationId)
                .OnDelete(ReferentialAction.Cascade);
        });
    }
}
