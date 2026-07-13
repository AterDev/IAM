using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEntitlementDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EntitlementCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntitlementType = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEntitlementDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserEntitlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntitlementDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValueLimit = table.Column<long>(type: "bigint", nullable: false),
                    CurrentValue = table.Column<long>(type: "bigint", nullable: false),
                    ExpirationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEntitlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEntitlements_UserEntitlementDefinitions_EntitlementDefi~",
                        column: x => x.EntitlementDefinitionId,
                        principalTable: "UserEntitlementDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEntitlementDefinitions_EntitlementCode",
                table: "UserEntitlementDefinitions",
                column: "EntitlementCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEntitlements_EntitlementDefinitionId",
                table: "UserEntitlements",
                column: "EntitlementDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEntitlements_UserId",
                table: "UserEntitlements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEntitlements_UserId_EntitlementDefinitionId",
                table: "UserEntitlements",
                columns: new[] { "UserId", "EntitlementDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEntitlements");

            migrationBuilder.DropTable(
                name: "UserEntitlementDefinitions");
        }
    }
}
