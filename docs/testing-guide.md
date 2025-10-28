# IAM Testing Guide

This document provides comprehensive information about testing in the IAM system.

## Test Structure

The IAM system includes two types of tests:

### 1. Unit Tests (`tests/Share.Tests.csproj`)

Located in the `tests/` directory, these tests verify individual components and business logic:

- **Services Tests**: Password hashing, JWT token generation, key management
- **Manager Tests**: Business logic for users, roles, clients, resources, scopes
- **OAuth Tests**: PKCE validation, token generation algorithms
- **Identity Tests**: User registration, role authorization, session management, audit logging, organization trees

**Running Unit Tests:**

```bash
cd tests
dotnet test
```

### 2. Integration Tests (`tests/Integration/`)

End-to-end tests using TestServer to validate complete workflows:

- **OAuth Flow Tests**: Authorization code flow, refresh token flow, PKCE
- **User CRUD Tests**: User creation, retrieval, update, deletion
- **Role CRUD Tests**: Role management and permission assignment
- **Client CRUD Tests**: OAuth client registration and management

**Running Integration Tests:**

```bash
cd tests/Integration
dotnet test
```

See [Integration Tests README](../tests/Integration/README.md) for detailed information.

## Prerequisites

- .NET 10 SDK RC2 or later
- All dependencies restored (`dotnet restore`)

## Running All Tests

From the repository root:

```bash
# Run all tests (unit + integration)
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Running Specific Test Categories

### By Project

```bash
# Unit tests only
dotnet test tests/Share.Tests.csproj

# Integration tests only
dotnet test tests/Integration/IntegrationTests.csproj
```

### By Namespace

```bash
# OAuth tests
dotnet test --filter "FullyQualifiedName~OAuth"

# User management tests
dotnet test --filter "FullyQualifiedName~User"

# Role management tests
dotnet test --filter "FullyQualifiedName~Role"

# Client management tests
dotnet test --filter "FullyQualifiedName~Client"
```

### By Test Name

```bash
# Run specific test
dotnet test --filter "FullyQualifiedName~AuthorizationCodeFlowTests.AuthorizationRequest_WithValidParameters_ReturnsAuthorizationCode"

# Run all tests containing "PKCE"
dotnet test --filter "DisplayName~PKCE"
```

## Continuous Integration

Tests are designed to run in CI/CD pipelines:

### GitHub Actions Example

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
        include-prerelease: true
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run Unit Tests
      run: dotnet test tests/Share.Tests.csproj --no-build --verbosity normal
    
    - name: Run Integration Tests
      run: dotnet test tests/Integration/IntegrationTests.csproj --no-build --verbosity normal
    
    - name: Generate Coverage Report
      run: dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Test Coverage

### Current Coverage Areas

✅ **OAuth/OIDC Flows**
- Authorization code flow with PKCE
- Refresh token flow and rotation
- Token generation and validation
- Client credentials flow
- Device flow

✅ **User Management**
- User CRUD operations
- Password validation and hashing
- User status and lockout
- Role assignment
- Registration workflows

✅ **Role Management**
- Role CRUD operations
- Permission assignment
- Role-based access control
- System role protection

✅ **Client Management**
- Client registration
- Secret rotation
- Scope assignment
- Authorization tracking
- Public vs confidential clients

✅ **Security**
- Password hashing (PBKDF2)
- JWT token signing and validation
- Key management and rotation
- Audit logging
- Session management

### Coverage Goals

Target coverage: **80%** for critical paths

Priority areas:
1. Authentication and authorization flows (90%+)
2. Security-critical operations (90%+)
3. Business logic (80%+)
4. API endpoints (75%+)

## Writing Tests

### Unit Test Example

```csharp
[Fact]
public void PasswordHasher_HashPassword_ReturnsValidHash()
{
    // Arrange
    var hasher = new PasswordHasherService();
    var password = "SecurePassword@123";
    
    // Act
    var hash = hasher.HashPassword(password);
    
    // Assert
    Assert.NotNull(hash);
    Assert.NotEmpty(hash);
    Assert.NotEqual(password, hash);
}
```

### Integration Test Example

```csharp
[Fact]
public async Task CreateUser_WithValidData_ReturnsCreated()
{
    // Arrange
    var client = await GetAuthenticatedClientAsync();
    var newUser = new { /* ... */ };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/users", newUser);
    
    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

## Test Best Practices

### 1. Test Naming

Use descriptive names following the pattern:
```
MethodUnderTest_Scenario_ExpectedBehavior
```

Examples:
- `CreateUser_WithValidData_ReturnsCreated`
- `TokenExchange_WithInvalidCode_ReturnsUnauthorized`
- `RotateSecret_WithValidClient_ReturnsNewSecret`

### 2. Test Organization

- **Arrange**: Set up test data and dependencies
- **Act**: Execute the method under test
- **Assert**: Verify the results

### 3. Test Isolation

- Each test should be independent
- Use in-memory database for integration tests
- Clean up test data between tests
- Don't rely on test execution order

### 4. Test Data

- Use realistic but safe test data
- Generate unique identifiers (GUIDs) to avoid conflicts
- Use constants for commonly used test values
- Don't use production data in tests

### 5. Assertions

- Test one concept per test
- Use specific assertions (Equal, NotNull, Contains, etc.)
- Include helpful assertion messages
- Verify both success and error cases

## Debugging Tests

### Visual Studio

1. Open Test Explorer (Test > Test Explorer)
2. Right-click on test > Debug
3. Set breakpoints in test or source code

### Visual Studio Code

1. Install C# Dev Kit extension
2. Open test file
3. Click "Debug Test" above test method
4. Set breakpoints as needed

### Command Line

```bash
# Run with debug output
dotnet test --logger "console;verbosity=detailed"

# Run specific test with debugging
VSTEST_HOST_DEBUG=1 dotnet test --filter "TestName"
```

## Common Issues

### Tests Fail in CI but Pass Locally

- Check .NET SDK version matches
- Verify all dependencies are restored
- Check for environment-specific configuration
- Review test database initialization

### Tests Are Slow

- Use in-memory database instead of real database
- Mock external dependencies
- Run tests in parallel where safe
- Profile tests to identify bottlenecks

### Tests Are Flaky

- Check for race conditions
- Ensure proper test isolation
- Verify async/await usage
- Remove dependencies on external systems

## Resources

- [xUnit Documentation](https://xunit.net/)
- [ASP.NET Core Integration Tests](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [Test-Driven Development](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [Moq Documentation](https://github.com/moq/moq4)

## Reporting Issues

If you encounter test failures:

1. Check that you're using the correct .NET SDK version
2. Verify all dependencies are restored
3. Review test output for error details
4. Check GitHub Issues for similar problems
5. Create a new issue with:
   - Test name and failure message
   - Steps to reproduce
   - Environment details (.NET version, OS, etc.)
   - Relevant logs or stack traces
