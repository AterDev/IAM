# IAM Integration Tests

This directory contains end-to-end integration tests for the IAM system using xUnit and ASP.NET Core TestServer.

## Overview

The integration tests validate the complete API workflows including:
- OAuth 2.0 / OIDC flows (authorization code, refresh token, client credentials)
- User CRUD operations
- Role CRUD and permission management
- OAuth Client registration and management

## Test Structure

```
Integration/
├── Infrastructure/
│   └── IAMWebApplicationFactory.cs    # Test server setup with in-memory database
├── OAuth/
│   ├── AuthorizationCodeFlowTests.cs  # OAuth authorization code flow tests
│   └── RefreshTokenFlowTests.cs       # Refresh token and rotation tests
├── Users/
│   └── UserCrudTests.cs               # User management API tests
├── Roles/
│   └── RoleCrudTests.cs               # Role management and permission tests
└── Clients/
    └── ClientCrudTests.cs             # OAuth client management tests
```

## Running the Tests

### Prerequisites

- .NET 10 SDK RC2 or later
- All dependencies restored

### Run All Integration Tests

```bash
cd tests/Integration
dotnet test
```

### Run Specific Test Category

```bash
# OAuth tests only
dotnet test --filter "FullyQualifiedName~IntegrationTests.OAuth"

# User CRUD tests only
dotnet test --filter "FullyQualifiedName~IntegrationTests.Users"

# Role CRUD tests only
dotnet test --filter "FullyQualifiedName~IntegrationTests.Roles"

# Client CRUD tests only
dotnet test --filter "FullyQualifiedName~IntegrationTests.Clients"
```

### Run with Verbose Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Generate Code Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Test Infrastructure

### IAMWebApplicationFactory

The `IAMWebApplicationFactory` class extends `WebApplicationFactory<Program>` to:
- Configure test environment with in-memory database
- Override DbContext to use EF Core In-Memory provider
- Seed test data for consistent test execution
- Provide isolated test environment

### IntegrationTestBase

Base class for all integration tests providing:
- Preconfigured HttpClient for API calls
- Helper method `GetAuthenticatedClientAsync()` to obtain JWT tokens
- Database cleanup utilities
- Common test fixtures

## Writing New Tests

### Example Test

```csharp
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Users;

public class MyNewTests : IntegrationTestBase
{
    public MyNewTests(IAMWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task MyTest_Description_ExpectedResult()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        
        // Act
        var response = await client.GetAsync("/api/endpoint");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

### Best Practices

1. **Test Isolation**: Each test should be independent and not rely on other tests
2. **Use In-Memory Database**: Tests use in-memory database that's reset between test classes
3. **Authentication**: Use `GetAuthenticatedClientAsync()` for endpoints requiring auth
4. **Descriptive Names**: Follow naming pattern `MethodUnderTest_Scenario_ExpectedBehavior`
5. **Assert HTTP Status**: Always verify response status codes
6. **Clean Test Data**: Use unique identifiers (GUIDs) to avoid conflicts

## Test Coverage Goals

The integration tests aim to cover:

- ✅ OAuth 2.0 Authorization Code Flow
- ✅ OAuth 2.0 Refresh Token Flow
- ✅ PKCE (Proof Key for Code Exchange) validation
- ✅ User CRUD operations with validation
- ✅ Role CRUD and permission assignment
- ✅ OAuth Client registration and management
- ✅ Client secret rotation
- ✅ Scope management
- ✅ Authentication and authorization
- ✅ Input validation and error handling

## Continuous Integration

These tests are designed to run in CI/CD pipelines:
- Fast execution using in-memory database
- No external dependencies required
- Parallel test execution supported
- Consistent results across environments

## Troubleshooting

### Tests Failing in CI

- Ensure .NET 10 SDK is installed
- Check that all NuGet packages are restored
- Verify test environment settings in appsettings.Test.json

### Database Initialization Errors

- The in-memory database is recreated for each test class
- Check that entity configurations are properly registered
- Verify DbContext is correctly configured in test setup

### Authentication Failures

- Ensure test users and clients are seeded in `IAMWebApplicationFactory.SeedTestData()`
- Check JWT configuration in test environment
- Verify client credentials match those used in tests

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [ASP.NET Core Integration Tests](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [EF Core In-Memory Provider](https://docs.microsoft.com/en-us/ef/core/providers/in-memory/)
