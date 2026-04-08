using EntityFramework.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Tests.IAMMod.Migrations;

public class UserEmailRequiredAndEmailOnlyLoginMigrationTests
{
    [Fact]
    public void Up_WhenMakingEmailRequired_BackfillsLegacyEmailsBeforeAlteringColumns()
    {
        var operations = TestUserEmailRequiredAndEmailOnlyLoginMigration.BuildUpOperations();

        var sqlOperations = operations.OfType<SqlOperation>().ToList();
        Assert.Equal(2, sqlOperations.Count);
        Assert.Contains(sqlOperations, op => op.Sql.Contains("UPDATE \"Users\"", StringComparison.Ordinal)
            && op.Sql.Contains("SET \"Email\" = CONCAT('legacy+'", StringComparison.Ordinal)
            && op.Sql.Contains("@default.local", StringComparison.Ordinal));
        Assert.Contains(sqlOperations, op => op.Sql.Contains("SET \"NormalizedEmail\" = UPPER(BTRIM(\"Email\"))", StringComparison.Ordinal));

        var alterOperations = operations.OfType<AlterColumnOperation>().ToList();
        Assert.Equal(2, alterOperations.Count);
        Assert.All(alterOperations, operation => Assert.False(operation.IsNullable));
        Assert.All(alterOperations, operation => Assert.Null(operation.DefaultValue));
    }

    private sealed class TestUserEmailRequiredAndEmailOnlyLoginMigration : UserEmailRequiredAndEmailOnlyLogin
    {
        public static IReadOnlyList<MigrationOperation> BuildUpOperations()
        {
            var migration = new TestUserEmailRequiredAndEmailOnlyLoginMigration();
            var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            migration.Up(migrationBuilder);
            return migrationBuilder.Operations;
        }
    }
}
