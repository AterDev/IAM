using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class RemovePermissionManagedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Permissions_OwnedClientId_Type_ManagedBy",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "ManagedBy",
                table: "Permissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManagedBy",
                table: "Permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_OwnedClientId_Type_ManagedBy",
                table: "Permissions",
                columns: new[] { "OwnedClientId", "Type", "ManagedBy" });
        }
    }
}
