using EntityFramework.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Tests.IAMMod.Migrations;

public class UserEmailRequiredAndEmailOnlyLoginMigrationTests
{
    [Fact]
    public void Up_ShouldCreateCoreIamTablesAndIndexes()
    {
        var operations = TestInitMigration.BuildUpOperations();

        var createTables = operations.OfType<CreateTableOperation>().ToList();
        Assert.Contains(createTables, operation => operation.Name == "ApiResources");
        Assert.Contains(createTables, operation => operation.Name == "Clients");
        Assert.Contains(createTables, operation => operation.Name == "Users");

        var indexOperations = operations.OfType<CreateIndexOperation>().ToList();
        Assert.Contains(indexOperations, operation => operation.Table == "ApiResources"
            && operation.Name == "IX_ApiResources_Name"
            && operation.IsUnique);
        Assert.Contains(indexOperations, operation => operation.Table == "Clients"
            && operation.Name == "IX_Clients_ClientId"
            && operation.IsUnique);
    }

    private sealed class TestInitMigration : Init
    {
        public static IReadOnlyList<MigrationOperation> BuildUpOperations()
        {
            var migration = new TestInitMigration();
            var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            migration.Up(migrationBuilder);
            return migrationBuilder.Operations;
        }
    }
}