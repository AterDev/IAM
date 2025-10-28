using AccessMod.Managers;
using AccessMod.Models.ScopeDtos;
using EntityFramework.DBProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Share.Tests.AccessMod;

/// <summary>
/// Integration tests for ScopeManager
/// </summary>
public class ScopeManagerTests : IDisposable
{
    private readonly DefaultDbContext _context;
    private readonly ScopeManager _manager;

    public ScopeManagerTests()
    {
        var options = new DbContextOptionsBuilder<DefaultDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DefaultDbContext(options);
        var logger = new LoggerFactory().CreateLogger<ScopeManager>();
        _manager = new ScopeManager(_context, logger);
    }

    [Fact]
    public async Task AddAsync_ValidScope_ReturnsDetail()
    {
        // Arrange
        var dto = new ScopeAddDto
        {
            Name = "read",
            DisplayName = "Read Access",
            Description = "Read access to resources",
            Required = false,
            Emphasize = false,
            Claims = new List<string> { "name", "email" }
        };

        // Act
        var detail = await _manager.AddAsync(dto);

        // Assert
        Assert.NotNull(detail);
        Assert.Equal("read", detail.Name);
        Assert.Equal("Read Access", detail.DisplayName);
        Assert.Equal(2, detail.Claims.Count);
        Assert.Contains("name", detail.Claims);
        Assert.Contains("email", detail.Claims);
    }

    [Fact]
    public async Task AddAsync_DuplicateName_ReturnsNull()
    {
        // Arrange
        var dto1 = new ScopeAddDto
        {
            Name = "duplicate",
            DisplayName = "First Scope"
        };
        var dto2 = new ScopeAddDto
        {
            Name = "duplicate",
            DisplayName = "Second Scope"
        };

        // Act
        await _manager.AddAsync(dto1);
        var result = await _manager.AddAsync(dto2);

        // Assert
        Assert.Null(result);
        Assert.Contains("already exists", _manager.ErrorMsg);
    }

    [Fact]
    public async Task GetDetailAsync_ExistingScope_ReturnsDetail()
    {
        // Arrange
        var addDto = new ScopeAddDto
        {
            Name = "get-test",
            DisplayName = "Get Test Scope"
        };
        var created = await _manager.AddAsync(addDto);

        // Act
        var detail = await _manager.GetDetailAsync(created!.Id);

        // Assert
        Assert.NotNull(detail);
        Assert.Equal("get-test", detail.Name);
        Assert.Equal("Get Test Scope", detail.DisplayName);
    }

    [Fact]
    public async Task UpdateAsync_ExistingScope_UpdatesSuccessfully()
    {
        // Arrange
        var addDto = new ScopeAddDto
        {
            Name = "update-test",
            DisplayName = "Original Name"
        };
        var created = await _manager.AddAsync(addDto);

        var updateDto = new ScopeUpdateDto
        {
            DisplayName = "Updated Name",
            Description = "Updated description",
            Required = true,
            Claims = new List<string> { "profile", "roles" }
        };

        // Act
        var updated = await _manager.UpdateAsync(created!.Id, updateDto);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.DisplayName);
        Assert.Equal("Updated description", updated.Description);
        Assert.True(updated.Required);
        Assert.Equal(2, updated.Claims.Count);
        Assert.Contains("profile", updated.Claims);
        Assert.Contains("roles", updated.Claims);
    }

    [Fact]
    public async Task DeleteAsync_ExistingScope_DeletesSuccessfully()
    {
        // Arrange
        var addDto = new ScopeAddDto
        {
            Name = "delete-test",
            DisplayName = "Delete Test Scope"
        };
        var created = await _manager.AddAsync(addDto);

        // Act
        var success = await _manager.DeleteAsync(created!.Id);

        // Assert
        Assert.True(success);

        var detail = await _manager.GetDetailAsync(created.Id);
        Assert.Null(detail);
    }

    [Fact]
    public async Task GetPageAsync_WithFilter_ReturnsFilteredResults()
    {
        // Arrange
        await _manager.AddAsync(new ScopeAddDto
        {
            Name = "read",
            DisplayName = "Read Scope",
            Required = true
        });
        await _manager.AddAsync(new ScopeAddDto
        {
            Name = "write",
            DisplayName = "Write Scope",
            Required = false
        });
        await _manager.AddAsync(new ScopeAddDto
        {
            Name = "admin",
            DisplayName = "Admin Scope",
            Required = true
        });

        var filter = new ScopeFilterDto
        {
            Required = true,
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await _manager.GetPageAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
