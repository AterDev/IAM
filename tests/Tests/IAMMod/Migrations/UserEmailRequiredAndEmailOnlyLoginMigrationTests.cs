using Npgsql;

namespace Tests.IAMMod.Migrations;

public class UserEmailRequiredAndEmailOnlyLoginMigrationTests
{
    [Fact]
    public async Task EnsureCreated_ShouldCreateCoreIamTablesAndIndexes()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(EnsureCreated_ShouldCreateCoreIamTablesAndIndexes));
        await using var connection = new NpgsqlConnection(dbContext.ConnectionString);
        await connection.OpenAsync();

        var tables = await ReadStringsAsync(
            connection,
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'");
        Assert.Contains("ApiResources", tables);
        Assert.Contains("Clients", tables);
        Assert.Contains("Users", tables);

        var indexes = await ReadStringsAsync(
            connection,
            "SELECT indexdef FROM pg_indexes WHERE schemaname = 'public'");
        Assert.Contains(indexes, index => index.Contains("ApiResources", StringComparison.Ordinal)
            && index.Contains("Name", StringComparison.Ordinal)
            && index.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(indexes, index => index.Contains("Clients", StringComparison.Ordinal)
            && (index.Contains("ClientId", StringComparison.Ordinal)
                || index.Contains("ClientIdentifier", StringComparison.Ordinal))
            && index.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<List<string>> ReadStringsAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }
        return values;
    }
}
