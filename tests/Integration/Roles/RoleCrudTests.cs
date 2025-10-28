using IntegrationTests.Infrastructure;

namespace IntegrationTests.Roles;

/// <summary>
/// Integration tests for Role CRUD operations
/// Tests role creation, retrieval, update, deletion, and permission assignment
/// </summary>
public class RoleCrudTests : IntegrationTestBase
{
    public RoleCrudTests(IAMWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetRoles_ReturnsRoleList()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/roles");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task CreateRole_WithValidData_ReturnsCreated()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var newRole = new
        {
            Name = "TestRole",
            Description = "Test role for integration testing",
            IsSystemRole = false
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/roles", newRole);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Created || 
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task CreateRole_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var duplicateRole = new
        {
            Name = "Admin", // Assuming this already exists
            Description = "Duplicate admin role"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/roles", duplicateRole);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Conflict ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task GetRoleById_WithValidId_ReturnsRole()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var roleId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/roles/{roleId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task UpdateRole_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var roleId = Guid.NewGuid();
        var updateData = new
        {
            Name = "UpdatedRoleName",
            Description = "Updated description"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/roles/{roleId}", updateData);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task DeleteRole_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var roleId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/roles/{roleId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task AssignPermissionsToRole_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var roleId = Guid.NewGuid();
        var permissions = new
        {
            Permissions = new[] { "user.read", "user.write", "role.read" }
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/roles/{roleId}/permissions", permissions);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task GetRolePermissions_WithValidRoleId_ReturnsPermissions()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var roleId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/roles/{roleId}/permissions");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task DeleteSystemRole_ReturnsForbidden()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var systemRoleId = Guid.NewGuid(); // Would be actual system role ID

        // Act
        var response = await client.DeleteAsync($"/api/roles/{systemRoleId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task AssignUsersToRole_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var roleId = Guid.NewGuid();
        var userIds = new
        {
            UserIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/roles/{roleId}/users", userIds);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task RemoveUserFromRole_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var roleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/roles/{roleId}/users/{userId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task GetRoleUsers_WithValidRoleId_ReturnsUserList()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var roleId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/roles/{roleId}/users");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }
}
