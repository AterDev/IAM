using AccessMod.Managers;
using AccessMod.Models.ResourceDtos;
using EntityFramework.DBProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Share.Tests.AccessMod;

/// <summary>
/// Integration tests for ResourceManager
/// </summary>
public class ResourceManagerTests : IDisposable
{
    private readonly DefaultDbContext _context;
    private readonly ResourceManager _manager;

    public ResourceManagerTests()
    {
        var options = new DbContextOptionsBuilder<DefaultDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DefaultDbContext(options);
        var logger = new LoggerFactory().CreateLogger<ResourceManager>();
        _manager = new ResourceManager(_context, logger);
    }

    [Fact]
    public async Task AddAsync_ValidResource_ReturnsDetail()
    {
        // Arrange
        var dto = new ResourceAddDto
        {
            Name = "api1",
            DisplayName = "API 1",
            Description = "First API resource"
        };

        // Act
        var detail = await _manager.AddAsync(dto);

        // Assert
        Assert.NotNull(detail);
        Assert.Equal("api1", detail.Name);
        Assert.Equal("API 1", detail.DisplayName);
        Assert.Equal("First API resource", detail.Description);
    }

    [Fact]
    public async Task AddAsync_DuplicateName_ReturnsNull()
    {
        // Arrange
        var dto1 = new ResourceAddDto
        {
            Name = "duplicate-api",
            DisplayName = "First API"
        };
        var dto2 = new ResourceAddDto
        {
            Name = "duplicate-api",
            DisplayName = "Second API"
        };

        // Act
        await _manager.AddAsync(dto1);
        var result = await _manager.AddAsync(dto2);

        // Assert
        Assert.Null(result);
        Assert.Contains("already exists", _manager.ErrorMsg);
    }

    [Fact]
    public async Task GetDetailAsync_ExistingResource_ReturnsDetail()
    {
        // Arrange
        var addDto = new ResourceAddDto
        {
            Name = "get-test-api",
            DisplayName = "Get Test API"
        };
        var created = await _manager.AddAsync(addDto);

        // Act
        var detail = await _manager.GetDetailAsync(created!.Id);

        // Assert
        Assert.NotNull(detail);
        Assert.Equal("get-test-api", detail.Name);
        Assert.Equal("Get Test API", detail.DisplayName);
    }

    [Fact]
    public async Task UpdateAsync_ExistingResource_UpdatesSuccessfully()
    {
        // Arrange
        var addDto = new ResourceAddDto
        {
            Name = "update-test-api",
            DisplayName = "Original Name"
        };
        var created = await _manager.AddAsync(addDto);

        var updateDto = new ResourceUpdateDto
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
    public async Task DeleteAsync_ExistingResource_DeletesSuccessfully()
    {
        // Arrange
        var addDto = new ResourceAddDto
        {
            Name = "delete-test-api",
            DisplayName = "Delete Test API"
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
        await _manager.AddAsync(new ResourceAddDto
        {
            Name = "api1",
            DisplayName = "First API"
        });
        await _manager.AddAsync(new ResourceAddDto
        {
            Name = "api2",
            DisplayName = "Second API"
        });
        await _manager.AddAsync(new ResourceAddDto
        {
            Name = "service1",
            DisplayName = "First Service"
        });

        var filter = new ResourceFilterDto
        {
            Name = "api",
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
