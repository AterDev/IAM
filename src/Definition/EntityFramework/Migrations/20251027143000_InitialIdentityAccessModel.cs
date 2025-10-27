using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntityFramework.Migrations;

/// <inheritdoc />
public partial class InitialIdentityAccessModel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Identity Module Tables

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                SecurityStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                IsTwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Organizations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                Level = table.Column<int>(type: "int", nullable: false),
                DisplayOrder = table.Column<int>(type: "int", nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Organizations", x => x.Id);
                table.ForeignKey(
                    name: "FK_Organizations_Organizations_ParentId",
                    column: x => x.ParentId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoles", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserRoles_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserRoles_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserClaims",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClaimType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ClaimValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserClaims_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RoleClaims",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClaimType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ClaimValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_RoleClaims_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserLogins",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ProviderDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserLogins", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserLogins_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "OrganizationUsers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrganizationUsers", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrganizationUsers_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_OrganizationUsers_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "LoginSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SessionId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                DeviceInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                LoginTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                LastActivityTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ExpirationTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoginSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_LoginSessions_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Access Module Tables

        migrationBuilder.CreateTable(
            name: "Clients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClientId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ClientSecret = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                RequirePkce = table.Column<bool>(type: "bit", nullable: false),
                ConsentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                ApplicationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Permissions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Settings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Clients", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ApiScopes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Resources = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Required = table.Column<bool>(type: "bit", nullable: false),
                Emphasize = table.Column<bool>(type: "bit", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiScopes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ApiResources",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiResources", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ClientRedirectUris",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Uri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientRedirectUris", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientRedirectUris_Clients_ClientId",
                    column: x => x.ClientId,
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClientPostLogoutRedirectUris",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Uri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientPostLogoutRedirectUris", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientPostLogoutRedirectUris_Clients_ClientId",
                    column: x => x.ClientId,
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Authorizations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SubjectId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Scopes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ExpirationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Authorizations", x => x.Id);
                table.ForeignKey(
                    name: "FK_Authorizations_Clients_ClientId",
                    column: x => x.ClientId,
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClientScopes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientScopes", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientScopes_Clients_ClientId",
                    column: x => x.ClientId,
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ClientScopes_ApiScopes_ScopeId",
                    column: x => x.ScopeId,
                    principalTable: "ApiScopes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ScopeClaims",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ScopeClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_ScopeClaims_ApiScopes_ScopeId",
                    column: x => x.ScopeId,
                    principalTable: "ApiScopes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Tokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AuthorizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ReferenceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                SubjectId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ExpirationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RedemptionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tokens_Authorizations_AuthorizationId",
                    column: x => x.AuthorizationId,
                    principalTable: "Authorizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create Indexes

        // User indexes
        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedUserName",
            table: "Users",
            column: "NormalizedUserName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedEmail",
            table: "Users",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "IX_Users_TenantId",
            table: "Users",
            column: "TenantId");

        // Role indexes
        migrationBuilder.CreateIndex(
            name: "IX_Roles_NormalizedName",
            table: "Roles",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Roles_TenantId",
            table: "Roles",
            column: "TenantId");

        // UserRole indexes
        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_UserId_RoleId",
            table: "UserRoles",
            columns: new[] { "UserId", "RoleId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_RoleId",
            table: "UserRoles",
            column: "RoleId");

        // UserClaim indexes
        migrationBuilder.CreateIndex(
            name: "IX_UserClaims_UserId",
            table: "UserClaims",
            column: "UserId");

        // RoleClaim indexes
        migrationBuilder.CreateIndex(
            name: "IX_RoleClaims_RoleId",
            table: "RoleClaims",
            column: "RoleId");

        // UserLogin indexes
        migrationBuilder.CreateIndex(
            name: "IX_UserLogins_LoginProvider_ProviderKey",
            table: "UserLogins",
            columns: new[] { "LoginProvider", "ProviderKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserLogins_UserId",
            table: "UserLogins",
            column: "UserId");

        // UserToken indexes
        migrationBuilder.CreateIndex(
            name: "IX_UserTokens_UserId_LoginProvider_Name",
            table: "UserTokens",
            columns: new[] { "UserId", "LoginProvider", "Name" },
            unique: true);

        // Organization indexes
        migrationBuilder.CreateIndex(
            name: "IX_Organizations_ParentId",
            table: "Organizations",
            column: "ParentId");

        migrationBuilder.CreateIndex(
            name: "IX_Organizations_TenantId",
            table: "Organizations",
            column: "TenantId");

        // OrganizationUser indexes
        migrationBuilder.CreateIndex(
            name: "IX_OrganizationUsers_OrganizationId_UserId",
            table: "OrganizationUsers",
            columns: new[] { "OrganizationId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OrganizationUsers_UserId",
            table: "OrganizationUsers",
            column: "UserId");

        // LoginSession indexes
        migrationBuilder.CreateIndex(
            name: "IX_LoginSessions_SessionId",
            table: "LoginSessions",
            column: "SessionId");

        migrationBuilder.CreateIndex(
            name: "IX_LoginSessions_UserId",
            table: "LoginSessions",
            column: "UserId");

        // Client indexes
        migrationBuilder.CreateIndex(
            name: "IX_Clients_ClientId",
            table: "Clients",
            column: "ClientId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Clients_TenantId",
            table: "Clients",
            column: "TenantId");

        // ClientRedirectUri indexes
        migrationBuilder.CreateIndex(
            name: "IX_ClientRedirectUris_ClientId",
            table: "ClientRedirectUris",
            column: "ClientId");

        // ClientPostLogoutRedirectUri indexes
        migrationBuilder.CreateIndex(
            name: "IX_ClientPostLogoutRedirectUris_ClientId",
            table: "ClientPostLogoutRedirectUris",
            column: "ClientId");

        // ApiScope indexes
        migrationBuilder.CreateIndex(
            name: "IX_ApiScopes_Name",
            table: "ApiScopes",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ApiScopes_TenantId",
            table: "ApiScopes",
            column: "TenantId");

        // ClientScope indexes
        migrationBuilder.CreateIndex(
            name: "IX_ClientScopes_ClientId_ScopeId",
            table: "ClientScopes",
            columns: new[] { "ClientId", "ScopeId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ClientScopes_ScopeId",
            table: "ClientScopes",
            column: "ScopeId");

        // ScopeClaim indexes
        migrationBuilder.CreateIndex(
            name: "IX_ScopeClaims_ScopeId",
            table: "ScopeClaims",
            column: "ScopeId");

        // ApiResource indexes
        migrationBuilder.CreateIndex(
            name: "IX_ApiResources_Name",
            table: "ApiResources",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ApiResources_TenantId",
            table: "ApiResources",
            column: "TenantId");

        // Authorization indexes
        migrationBuilder.CreateIndex(
            name: "IX_Authorizations_SubjectId",
            table: "Authorizations",
            column: "SubjectId");

        migrationBuilder.CreateIndex(
            name: "IX_Authorizations_ClientId",
            table: "Authorizations",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_Authorizations_TenantId",
            table: "Authorizations",
            column: "TenantId");

        // Token indexes
        migrationBuilder.CreateIndex(
            name: "IX_Tokens_ReferenceId",
            table: "Tokens",
            column: "ReferenceId");

        migrationBuilder.CreateIndex(
            name: "IX_Tokens_AuthorizationId",
            table: "Tokens",
            column: "AuthorizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Tokens_TenantId",
            table: "Tokens",
            column: "TenantId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserRoles");
        migrationBuilder.DropTable(name: "UserClaims");
        migrationBuilder.DropTable(name: "RoleClaims");
        migrationBuilder.DropTable(name: "UserLogins");
        migrationBuilder.DropTable(name: "UserTokens");
        migrationBuilder.DropTable(name: "OrganizationUsers");
        migrationBuilder.DropTable(name: "LoginSessions");
        migrationBuilder.DropTable(name: "ClientRedirectUris");
        migrationBuilder.DropTable(name: "ClientPostLogoutRedirectUris");
        migrationBuilder.DropTable(name: "ClientScopes");
        migrationBuilder.DropTable(name: "ScopeClaims");
        migrationBuilder.DropTable(name: "Tokens");
        migrationBuilder.DropTable(name: "ApiResources");

        migrationBuilder.DropTable(name: "Roles");
        migrationBuilder.DropTable(name: "Organizations");
        migrationBuilder.DropTable(name: "Users");
        migrationBuilder.DropTable(name: "ApiScopes");
        migrationBuilder.DropTable(name: "Authorizations");
        migrationBuilder.DropTable(name: "Clients");
    }
}
