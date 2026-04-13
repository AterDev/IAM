using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClientPasswordGrantPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowPasswordGrant",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PasswordGrantRestrictionReason",
                table: "Clients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowPasswordGrant",
                table: "Clients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordGrantRestrictionReason",
                table: "Clients",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
