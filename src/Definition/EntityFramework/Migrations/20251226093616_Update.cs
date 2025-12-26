using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Tenants");

            migrationBuilder.AddColumn<string>(
                name: "AnalysisConnectionString",
                table: "Tenants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Disabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Domain",
                table: "Tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Domain",
                table: "Tenants",
                column: "Domain",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Domain",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AnalysisConnectionString",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Disabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Domain",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Tenants");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Tenants",
                type: "uuid",
                nullable: true);
        }
    }
}
