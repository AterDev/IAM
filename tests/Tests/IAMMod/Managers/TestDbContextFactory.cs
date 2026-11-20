using EntityFramework.AppDbContext;
using Npgsql;
using Perigon.PostgreSQL.Metadata;
using Perigon.PostgreSQL.Options;

namespace Tests.IAMMod.Managers;

/// <summary>Creates an isolated PostgreSQL database for each test.</summary>
internal static class TestDbContextFactory
{
    private static readonly string BaseConnectionString =
        Environment.GetEnvironmentVariable("IAM_TEST_POSTGRES")
        ?? Environment.GetEnvironmentVariable("IAM_TEST_CONNECTION")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__Test")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? "Host=localhost;Port=5432;Database=IAM;Username=postgres;Password=root;Include Error Detail=true";

    public static TestDbContext Create(string testName)
    {
        var databaseName = CreateDatabaseName(testName);
        var connectionBuilder = new NpgsqlConnectionStringBuilder(BaseConnectionString)
        {
            Database = databaseName,
        };

        CreateDatabase(connectionBuilder.Database);
        var options = new DbContextOptionsBuilder()
            .UsePostgres(connectionBuilder.ConnectionString)
            .Build<DefaultDbContext>();
        var context = new TestDbContext(options, connectionBuilder.ConnectionString, BaseConnectionString);
        context.EnsureCreatedAsync().GetAwaiter().GetResult();
        return context;
    }

    private static string CreateDatabaseName(string testName)
    {
        var safeName = new string(testName
            .Where(char.IsLetterOrDigit)
            .ToArray());
        if (safeName.Length > 36)
        {
            safeName = safeName[..36];
        }

        return $"iam_test_{safeName.ToLowerInvariant()}_{Guid.NewGuid():N}"[..63];
    }

    private static void CreateDatabase(string databaseName)
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(BaseConnectionString);
        using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)}";
        command.ExecuteNonQuery();
    }

    internal static async Task DropDatabaseAsync(string databaseName, string adminConnectionString)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)} WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"")}\"";
}

internal sealed class TestDbContext(
    DbContextOptions<DefaultDbContext> options,
    string connectionString,
    string adminConnectionString
) : DefaultDbContext(options)
{
    static TestDbContext()
    {
        // Perigon registers generated models by the concrete context type. This
        // test-only derived context shares DefaultDbContext's model and DbSets,
        // so copy that registration before the base constructor builds a model.
        EntityModelRegistry.RegisterContext<TestDbContext>(
            EntityModelRegistry.GetContextModels(typeof(DefaultDbContext)));
    }

    private int _disposed;
    private readonly string _databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database
        ?? throw new InvalidOperationException("Test database name is required.");

    public string ConnectionString { get; } = connectionString;

    public new async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            await base.DisposeAsync();
            await TestDbContextFactory.DropDatabaseAsync(_databaseName, adminConnectionString);
        }
    }

    public new void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            base.Dispose();
            TestDbContextFactory.DropDatabaseAsync(_databaseName, adminConnectionString)
                .GetAwaiter()
                .GetResult();
        }
    }
}
