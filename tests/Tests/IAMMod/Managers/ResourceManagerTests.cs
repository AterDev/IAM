using Microsoft.AspNetCore.Http;
using IAMMod.Models.ResourceDtos;

namespace Tests.IAMMod.Managers;

public class ResourceManagerTests
{
    [Fact]
    public async Task AddAsync_WithUniqueName_PersistsResource()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(AddAsync_WithUniqueName_PersistsResource));
        var manager = CreateManager(dbContext);

        var result = await manager.AddAsync(new ResourceAddDto
        {
            Name = "orders-api",
            DisplayName = "Orders API",
            Description = "Order management resource",
        });

        Assert.NotNull(result);
        Assert.Equal("orders-api", result!.Name);
        Assert.Equal("Orders API", result.DisplayName);

        var entity = await dbContext.ApiResources.SingleAsync(r => r.Name == "orders-api");
        Assert.Equal("Orders API", entity.DisplayName);
        Assert.Equal("Order management resource", entity.Description);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateName_ThrowsBusinessException()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(AddAsync_WithDuplicateName_ThrowsBusinessException));
        dbContext.ApiResources.Add(new ApiResource
        {
            Name = "orders-api",
            DisplayName = "Orders API",
        });
        await dbContext.SaveChangesAsync();

        var manager = CreateManager(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.AddAsync(new ResourceAddDto
        {
            Name = "orders-api",
            DisplayName = "Orders API Duplicate",
        }));

        Assert.Equal("ResourceNameExists", exception.LanguageKey);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCodes);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingResource_UpdatesValues()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(UpdateAsync_WithExistingResource_UpdatesValues));
        var resource = new ApiResource
        {
            Name = "orders-api",
            DisplayName = "Orders API",
            Description = "Original description",
        };
        dbContext.ApiResources.Add(resource);
        await dbContext.SaveChangesAsync();

        var manager = CreateManager(dbContext);

        var result = await manager.UpdateAsync(resource.Id, new ResourceUpdateDto
        {
            DisplayName = "Orders API Updated",
            Description = "Updated description",
        });

        Assert.NotNull(result);
        Assert.Equal("Orders API Updated", result!.DisplayName);
        Assert.Equal("Updated description", result.Description);

        var entity = await dbContext.ApiResources.SingleAsync(r => r.Id == resource.Id);
        Assert.Equal("Orders API Updated", entity.DisplayName);
        Assert.Equal("Updated description", entity.Description);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingResource_SoftDeletesResource()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(DeleteAsync_WithExistingResource_SoftDeletesResource));
        var resource = new ApiResource
        {
            Name = "orders-api",
            DisplayName = "Orders API",
        };
        dbContext.ApiResources.Add(resource);
        await dbContext.SaveChangesAsync();

        var manager = CreateManager(dbContext);

        var deleted = await manager.DeleteAsync(resource.Id);

        Assert.True(deleted);

        var entity = await dbContext.ApiResources.IgnoreQueryFilters().SingleAsync(r => r.Id == resource.Id);
        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_WithMissingResource_ThrowsBusinessException()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(DeleteAsync_WithMissingResource_ThrowsBusinessException));
        var manager = CreateManager(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.DeleteAsync(Guid.NewGuid()));

        Assert.Equal("ResourceNotFound", exception.LanguageKey);
        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCodes);
    }

    private static ResourceManager CreateManager(DefaultDbContext dbContext)
    {
        return ManagerTestFactory.Create<ResourceManager, ApiResource>(dbContext);
    }
}
