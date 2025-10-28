using AccessMod.Managers;
using AccessMod.Models.ClientDtos;
using Entity.Access;
using EntityFramework.DBProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Share.Services;
using Xunit;

namespace Share.Tests.AccessMod;

/// <summary>
/// Integration tests for ClientManager
/// </summary>
public class ClientManagerTests : IDisposable
{
    private readonly DefaultDbContext _context;
    private readonly ClientManager _manager;
    private readonly IPasswordHasher _passwordHasher;

    public ClientManagerTests()
    {
        var options = new DbContextOptionsBuilder<DefaultDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DefaultDbContext(options);
        _passwordHasher = new PasswordHasherService();
        var logger = new LoggerFactory().CreateLogger<ClientManager>();
        _manager = new ClientManager(_context, _passwordHasher, logger);
    }

    [Fact]
    public async Task AddAsync_ValidClient_ReturnsClientWithSecret()
    {
        // Arrange
        var dto = new ClientAddDto
        {
            ClientId = "test-client",
            DisplayName = "Test Client",
            Description = "A test client",
            Type = "confidential",
            RequirePkce = true,
            ConsentType = "explicit",
            ApplicationType = "web"
        };

        // Act
        var (detail, secret) = await _manager.AddAsync(dto);

        // Assert
        Assert.NotNull(detail);
        Assert.NotNull(secret);
        Assert.Equal("test-client", detail.ClientId);
        Assert.Equal("Test Client", detail.DisplayName);
        Assert.NotEmpty(secret);
    }

    [Fact]
    public async Task AddAsync_DuplicateClientId_ReturnsNull()
    {
        // Arrange
        var dto1 = new ClientAddDto
        {
            ClientId = "duplicate-client",
            DisplayName = "First Client"
        };
        var dto2 = new ClientAddDto
        {
            ClientId = "duplicate-client",
            DisplayName = "Second Client"
        };

        // Act
        await _manager.AddAsync(dto1);
        var (detail, secret) = await _manager.AddAsync(dto2);

        // Assert
        Assert.Null(detail);
        Assert.Null(secret);
        Assert.Contains("already exists", _manager.ErrorMsg);
    }

    [Fact]
    public async Task GetDetailAsync_ExistingClient_ReturnsDetail()
    {
        // Arrange
        var addDto = new ClientAddDto
        {
            ClientId = "get-test-client",
            DisplayName = "Get Test Client"
        };
        var (created, _) = await _manager.AddAsync(addDto);

        // Act
        var detail = await _manager.GetDetailAsync(created!.Id);

        // Assert
        Assert.NotNull(detail);
        Assert.Equal("get-test-client", detail.ClientId);
        Assert.Equal("Get Test Client", detail.DisplayName);
    }

    [Fact]
    public async Task UpdateAsync_ExistingClient_UpdatesSuccessfully()
    {
        // Arrange
        var addDto = new ClientAddDto
        {
            ClientId = "update-client",
            DisplayName = "Original Name"
        };
        var (created, _) = await _manager.AddAsync(addDto);

        var updateDto = new ClientUpdateDto
        {
            DisplayName = "Updated Name",
            Description = "Updated description"
        };

        // Act
        var updated = await _manager.UpdateAsync(created!.Id, updateDto);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.DisplayName);
        Assert.Equal("Updated description", updated.Description);
    }

    [Fact]
    public async Task RotateSecretAsync_ExistingClient_ReturnsNewSecret()
    {
        // Arrange
        var addDto = new ClientAddDto
        {
            ClientId = "rotate-client",
            DisplayName = "Rotate Test Client"
        };
        var (created, originalSecret) = await _manager.AddAsync(addDto);

        // Act
        var newSecret = await _manager.RotateSecretAsync(created!.Id);

        // Assert
        Assert.NotNull(newSecret);
        Assert.NotEqual(originalSecret, newSecret);
    }

    [Fact]
    public async Task AssignScopesAsync_WithValidScopes_AssignsSuccessfully()
    {
        // Arrange
        // Create a client
        var clientDto = new ClientAddDto
        {
            ClientId = "scope-test-client",
            DisplayName = "Scope Test Client"
        };
        var (client, _) = await _manager.AddAsync(clientDto);

        // Create scopes
        var scope1 = new ApiScope
        {
            Name = "read",
            DisplayName = "Read Access"
        };
        var scope2 = new ApiScope
        {
            Name = "write",
            DisplayName = "Write Access"
        };
        _context.Set<ApiScope>().AddRange(scope1, scope2);
        await _context.SaveChangesAsync();

        // Act
        var success = await _manager.AssignScopesAsync(
            client!.Id,
            new List<Guid> { scope1.Id, scope2.Id }
        );

        // Assert
        Assert.True(success);

        // Verify scopes were assigned
        var detail = await _manager.GetDetailAsync(client.Id);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.Scopes.Count);
        Assert.Contains("read", detail.Scopes);
        Assert.Contains("write", detail.Scopes);
    }

    [Fact]
    public async Task DeleteAsync_ExistingClient_DeletesSuccessfully()
    {
        // Arrange
        var addDto = new ClientAddDto
        {
            ClientId = "delete-client",
            DisplayName = "Delete Test Client"
        };
        var (created, _) = await _manager.AddAsync(addDto);

        // Act
        var success = await _manager.DeleteAsync(created!.Id);

        // Assert
        Assert.True(success);

        var detail = await _manager.GetDetailAsync(created.Id);
        Assert.Null(detail);
    }

    [Fact]
    public async Task GetAuthorizationsAsync_ReturnsClientAuthorizations()
    {
        // Arrange
        var clientDto = new ClientAddDto
        {
            ClientId = "auth-test-client",
            DisplayName = "Auth Test Client"
        };
        var (client, _) = await _manager.AddAsync(clientDto);

        // Create some authorizations
        var authorization = new Authorization
        {
            ClientId = client!.Id,
            SubjectId = "user123",
            Type = "permanent",
            Status = "valid",
            Scopes = "read write",
            CreationDate = DateTimeOffset.UtcNow
        };
        _context.Set<Authorization>().Add(authorization);
        await _context.SaveChangesAsync();

        // Act
        var authorizations = await _manager.GetAuthorizationsAsync(client.Id);

        // Assert
        Assert.NotEmpty(authorizations);
        Assert.Single(authorizations);
        Assert.Equal("user123", authorizations[0].SubjectId);
        Assert.Equal("valid", authorizations[0].Status);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
