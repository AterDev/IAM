using Microsoft.AspNetCore.Http;
using IAMMod.Models.OrganizationDtos;

namespace Tests.IAMMod.Managers;

public class OrganizationManagerTests
{
    [Fact]
    public async Task UpdateAsync_WhenMovingOnlyToParentWithSameName_ThrowsBeforeMutation()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(UpdateAsync_WhenMovingOnlyToParentWithSameName_ThrowsBeforeMutation));
        var oldParent = new Organization { Name = "Old Parent" };
        var newParent = new Organization { Name = "New Parent" };
        var organization = new Organization
        {
            Name = "Engineering",
            ParentId = oldParent.Id,
        };
        var targetSibling = new Organization
        {
            Name = organization.Name,
            ParentId = newParent.Id,
        };

        dbContext.Organizations.AddRange(oldParent, newParent, organization, targetSibling);
        await dbContext.SaveChangesAsync();

        var manager = ManagerTestFactory.Create<OrganizationManager, Organization>(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.UpdateAsync(
            organization.Id,
            new OrganizationUpdateDto { ParentId = newParent.Id }));

        Assert.Equal("OrganizationNameExists", exception.LanguageKey);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCodes);

        var persisted = await dbContext.Organizations.SingleAsync(item => item.Id == organization.Id);
        Assert.Equal(oldParent.Id, persisted.ParentId);
        Assert.Equal("Engineering", persisted.Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenMovingAndRenamingToTargetDuplicate_ThrowsBeforeMutation()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(UpdateAsync_WhenMovingAndRenamingToTargetDuplicate_ThrowsBeforeMutation));
        var oldParent = new Organization { Name = "Old Parent" };
        var newParent = new Organization { Name = "New Parent" };
        var organization = new Organization
        {
            Name = "Legacy Engineering",
            ParentId = oldParent.Id,
        };
        var targetSibling = new Organization
        {
            Name = "Engineering",
            ParentId = newParent.Id,
        };

        dbContext.Organizations.AddRange(oldParent, newParent, organization, targetSibling);
        await dbContext.SaveChangesAsync();

        var manager = ManagerTestFactory.Create<OrganizationManager, Organization>(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.UpdateAsync(
            organization.Id,
            new OrganizationUpdateDto
            {
                Name = "Engineering",
                ParentId = newParent.Id,
            }));

        Assert.Equal("OrganizationNameExists", exception.LanguageKey);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCodes);

        var persisted = await dbContext.Organizations.SingleAsync(item => item.Id == organization.Id);
        Assert.Equal(oldParent.Id, persisted.ParentId);
        Assert.Equal("Legacy Engineering", persisted.Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenOldParentHasTargetNameButNewParentDoesNot_MovesAndRenames()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(UpdateAsync_WhenOldParentHasTargetNameButNewParentDoesNot_MovesAndRenames));
        var oldParent = new Organization { Name = "Old Parent" };
        var newParent = new Organization { Name = "New Parent" };
        var organization = new Organization
        {
            Name = "Legacy Engineering",
            ParentId = oldParent.Id,
        };
        var oldParentSibling = new Organization
        {
            Name = "Engineering",
            ParentId = oldParent.Id,
        };

        dbContext.Organizations.AddRange(oldParent, newParent, organization, oldParentSibling);
        await dbContext.SaveChangesAsync();

        var manager = ManagerTestFactory.Create<OrganizationManager, Organization>(dbContext);

        var result = await manager.UpdateAsync(
            organization.Id,
            new OrganizationUpdateDto
            {
                Name = "Engineering",
                ParentId = newParent.Id,
            });

        Assert.NotNull(result);
        Assert.Equal("Engineering", result!.Name);
        Assert.Equal(newParent.Id, result.ParentId);

        var persisted = await dbContext.Organizations.SingleAsync(item => item.Id == organization.Id);
        Assert.Equal(newParent.Id, persisted.ParentId);
        Assert.Equal("Engineering", persisted.Name);
    }
}
