
namespace EntityFramework.DBProvider;

public partial class DefaultDbContext(DbContextOptions<DefaultDbContext> options)
    : ContextBase(options)
{
    // Common entities
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<SigningKey> SigningKeys { get; set; }

    // Identity entities
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserClaim> UserClaims { get; set; }
    public DbSet<RoleClaim> RoleClaims { get; set; }
    public DbSet<UserLogin> UserLogins { get; set; }
    public DbSet<UserToken> UserTokens { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationUser> OrganizationUsers { get; set; }
    public DbSet<LoginSession> LoginSessions { get; set; }

    // Access entities
    public DbSet<Client> Clients { get; set; }
    public DbSet<ApiScope> ApiScopes { get; set; }
    public DbSet<ClientScope> ClientScopes { get; set; }
    public DbSet<ScopeClaim> ScopeClaims { get; set; }
    public DbSet<ApiResource> ApiResources { get; set; }
    public DbSet<Authorization> Authorizations { get; set; }
    public DbSet<Token> Tokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
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
            entity.HasIndex(e => e.TenantId);
        });
    }

    private void ConfigureIdentityEntities(ModelBuilder builder)
    {
        // User configuration
        builder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.NormalizedUserName).IsUnique();
            entity.HasIndex(e => e.NormalizedEmail);
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
        });

        // Role configuration
        builder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
        });

        // UserRole configuration
        builder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserClaim configuration
        builder.Entity<UserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserClaims)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RoleClaim configuration
        builder.Entity<RoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RoleClaims)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserLogin configuration
        builder.Entity<UserLogin>(entity =>
        {
            entity.HasIndex(e => new { e.LoginProvider, e.ProviderKey }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserLogins)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserToken configuration
        builder.Entity<UserToken>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.LoginProvider, e.Name }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Organization configuration
        builder.Entity<Organization>(entity =>
        {
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Parent)
                .WithMany(o => o.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrganizationUser configuration
        builder.Entity<OrganizationUser>(entity =>
        {
            entity.HasIndex(e => new { e.OrganizationId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.OrganizationUsers)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.OrganizationUsers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LoginSession configuration
        builder.Entity<LoginSession>(entity =>
        {
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.LoginSessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAccessEntities(ModelBuilder builder)
    {
        // Client configuration
        builder.Entity<Client>(entity =>
        {
            entity.HasIndex(e => e.ClientId).IsUnique();
            entity.HasIndex(e => e.TenantId);
            
            // Configure RedirectUris as JSON
            entity.Property(e => e.RedirectUris)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                );
            
            // Configure PostLogoutRedirectUris as JSON
            entity.Property(e => e.PostLogoutRedirectUris)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                );
        });

        // ApiScope configuration
        builder.Entity<ApiScope>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // ClientScope configuration
        builder.Entity<ClientScope>(entity =>
        {
            entity.HasIndex(e => new { e.ClientId, e.ScopeId }).IsUnique();
            entity.HasOne(e => e.Client)
                .WithMany(c => c.ClientScopes)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Scope)
                .WithMany(s => s.ClientScopes)
                .HasForeignKey(e => e.ScopeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ScopeClaim configuration
        builder.Entity<ScopeClaim>(entity =>
        {
            entity.HasIndex(e => e.ScopeId);
            entity.HasOne(e => e.Scope)
                .WithMany(s => s.ScopeClaims)
                .HasForeignKey(e => e.ScopeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiResource configuration
        builder.Entity<ApiResource>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // Authorization configuration
        builder.Entity<Authorization>(entity =>
        {
            entity.HasIndex(e => e.SubjectId);
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Client)
                .WithMany(c => c.Authorizations)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Token configuration
        builder.Entity<Token>(entity =>
        {
            entity.HasIndex(e => e.ReferenceId);
            entity.HasIndex(e => e.AuthorizationId);
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Authorization)
                .WithMany(a => a.Tokens)
                .HasForeignKey(e => e.AuthorizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
