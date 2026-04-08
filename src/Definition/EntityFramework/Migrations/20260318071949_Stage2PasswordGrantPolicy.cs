using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class Stage2PasswordGrantPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowPasswordGrant",
                table: "Clients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "DeveloperUserId",
                table: "Clients",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordGrantRestrictionReason",
                table: "Clients",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegistrationStatus",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RequestedTime",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "Clients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedTime",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SecretExpiresAt",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClientSecrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecretHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SecretSalt = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastFour = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientSecrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientSecrets_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_DeveloperUserId",
                table: "Clients",
                column: "DeveloperUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_RegistrationStatus",
                table: "Clients",
                column: "RegistrationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ClientSecrets_ClientId",
                table: "ClientSecrets",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientSecrets_ClientId_RevokedAt",
                table: "ClientSecrets",
                columns: new[] { "ClientId", "RevokedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientSecrets");

            migrationBuilder.DropIndex(
                name: "IX_Clients_DeveloperUserId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_RegistrationStatus",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "AllowPasswordGrant",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DeveloperUserId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PasswordGrantRestrictionReason",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RegistrationStatus",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RequestedTime",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ReviewedTime",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "SecretExpiresAt",
                table: "Clients");
        }
    }
}
