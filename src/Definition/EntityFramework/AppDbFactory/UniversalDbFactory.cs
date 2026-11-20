using Microsoft.Extensions.Configuration;
using Perigon.PostgreSQL;
using Perigon.PostgreSQL.Options;
using System.Diagnostics.CodeAnalysis;

namespace EntityFramework.AppDbFactory;

/// <summary>Creates a context for a configured application database.</summary>
public sealed class UniversalDbFactory(IConfiguration configuration)
{
    public TContext CreateDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TContext>() where TContext : DbContext
    {
        var contextName = typeof(TContext).Name;
        if (contextName.EndsWith("DbContext", StringComparison.Ordinal))
            contextName = contextName[..^"DbContext".Length];

        var connectionString = configuration.GetConnectionString(contextName)
            ?? throw new InvalidOperationException($"Connection string for {contextName} not found.");
        var options = new DbContextOptionsBuilder().UsePostgres(connectionString).Build<TContext>();
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }
}
