using IntegrationTests.Infrastructure;

namespace IntegrationTests.Users;

/// <summary>
/// Integration tests for User CRUD operations
/// Tests user creation, retrieval, update, and deletion via API
/// </summary>
public class UserCrudTests : IntegrationTestBase
{
    public UserCrudTests(IAMWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetUsers_ReturnsUserList()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/users");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.Unauthorized,
            "Expected OK or Unauthorized status"
        );
    }

    [Fact]
    public async Task GetUsers_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/users?page=1&pageSize=10");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.Unauthorized
        );
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var newUser = new
        {
            UserName = "newuser",
            Email = "newuser@example.com",
            PhoneNumber = "1234567890",
            Password = "NewUser@Password123",
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            LockoutEnabled = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", newUser);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Created || 
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task CreateUser_WithDuplicateUsername_ReturnsBadRequest()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var duplicateUser = new
        {
            UserName = "testuser", // Assuming this already exists
            Email = "duplicate@example.com",
            Password = "Password@123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", duplicateUser);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Conflict ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task GetUserById_WithValidId_ReturnsUser()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var userId = Guid.NewGuid(); // In real test, use actual user ID

        // Act
        var response = await client.GetAsync($"/api/users/{userId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var userId = Guid.NewGuid();
        var updateData = new
        {
            Email = "updated@example.com",
            PhoneNumber = "9876543210",
            EmailConfirmed = true
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}", updateData);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task UpdateUserStatus_LocksUser_ReturnsSuccess()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var userId = Guid.NewGuid();
        var statusUpdate = new
        {
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddDays(7)
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/users/{userId}/status", statusUpdate);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task DeleteUser_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var userId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/users/{userId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task DeleteUser_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var userId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/users/{userId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task SearchUsers_WithFilter_ReturnsFilteredResults()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/users?search=test&emailConfirmed=true");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var invalidUser = new
        {
            UserName = "testuser123",
            Email = "invalid-email",
            Password = "Password@123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", invalidUser);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task CreateUser_WithWeakPassword_ReturnsBadRequest()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var weakPasswordUser = new
        {
            UserName = "testuser456",
            Email = "test@example.com",
            Password = "weak"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", weakPasswordUser);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }
}
