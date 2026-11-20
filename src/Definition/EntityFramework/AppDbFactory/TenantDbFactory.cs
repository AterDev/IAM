using EntityFramework.AppDbContext;
using Microsoft.Extensions.Configuration;
using Perigon.AspNetCore.Constants;

namespace EntityFramework.AppDbFactory;

/// <summary>Compatibility wrapper for callers being migrated to the single application database.</summary>
public sealed class TenantDbFactory(IConfiguration configuration)
{
    public DefaultDbContext CreateDbContext()
        => AppDbFactory.CreateDbContext(configuration.GetConnectionString(AppConst.Default)
            ?? throw new InvalidOperationException("The default database connection is missing."));

    public Task<DefaultDbContext> CreateDbContextAsync()
        => Task.FromResult(CreateDbContext());
}
