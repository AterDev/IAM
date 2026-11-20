using EntityFramework.AppDbContext;
using Microsoft.Extensions.Configuration;
using Perigon.AspNetCore.Constants;
using Perigon.PostgreSQL.Options;

namespace EntityFramework.AppDbFactory;

/// <summary>Creates contexts for the single configured application database.</summary>
public sealed class AppDbFactory(IConfiguration configuration)
{
    public DefaultDbContext CreateDbContext()
        => CreateDbContext(GetConnectionString(AppConst.Default));

    public static DefaultDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder()
            .UsePostgres(connectionString)
            .Build<DefaultDbContext>();
        return new DefaultDbContext(options);
    }

    public Task<DefaultDbContext> CreateDbContextAsync()
        => Task.FromResult(CreateDbContext());

    private string GetConnectionString(string name)
        => configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"No connection string configured for '{name}'.");
}
