# Database Migrations

## Initial Identity and Access Model Migration

The initial migration `20251027143000_InitialIdentityAccessModel` includes:

### Identity Module Tables
- **Users**: Core user identity table with authentication fields
- **Roles**: Role definitions for authorization
- **UserRoles**: Many-to-many relationship between users and roles
- **UserClaims**: Custom claims associated with users
- **RoleClaims**: Custom claims associated with roles
- **UserLogins**: External authentication provider logins
- **UserTokens**: Authentication tokens for users
- **Organizations**: Hierarchical organization structure
- **OrganizationUsers**: Many-to-many relationship between organizations and users
- **LoginSessions**: Active user session tracking

### Access Module Tables (OAuth/OIDC)
- **Clients**: OAuth/OIDC client applications
- **ClientRedirectUris**: Allowed redirect URIs for clients
- **ClientPostLogoutRedirectUris**: Allowed post-logout redirect URIs
- **ApiScopes**: API scope definitions
- **ClientScopes**: Many-to-many relationship between clients and scopes
- **ScopeClaims**: Claims associated with scopes
- **ApiResources**: API resource definitions
- **Authorizations**: OAuth authorization grants
- **Tokens**: Access, refresh, and other token types

## Features
- **Soft Delete**: All entities inherit from EntityBase with IsDeleted flag
- **Multi-tenancy**: TenantId support on all entities
- **Concurrency Control**: ConcurrencyStamp on User and Role entities
- **Indexes**: Optimized indexes for performance on lookup fields
- **Foreign Keys**: Proper cascading deletes and restrictions

## Running Migrations

Since this project uses .NET 10.0 RC, ensure you have the correct SDK installed:

```bash
# Check SDK version
dotnet --version

# Run migrations via MigrationService
cd src/Services/MigrationService
dotnet run
```

Alternatively, use EF Core tools directly:

```bash
# Navigate to EntityFramework project
cd src/Definition/EntityFramework

# Update database
dotnet ef database update --startup-project ../../Services/ApiService

# Create new migration
dotnet ef migrations add <MigrationName> --startup-project ../../Services/ApiService
```

## Notes
- The migration script is designed for SQL Server but can be adapted for PostgreSQL or SQLite
- All timestamps use DateTimeOffset for timezone-aware storage
- Unique indexes on normalized names for case-insensitive lookups
- Cascading deletes configured for dependent entities
