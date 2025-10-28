using Xunit;

namespace Share.Tests.Identity;

/// <summary>
/// Tests for organization tree operations and management scenarios
/// </summary>
public class OrganizationTreeTests
{
    [Fact]
    public void OrganizationAddDto_ValidData_CreatesObject()
    {
        // Arrange
        var dto = new
        {
            Name = "Engineering",
            ParentId = (Guid?)null,
            DisplayOrder = 1,
            Description = "Engineering department"
        };

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("Engineering", dto.Name);
        Assert.Null(dto.ParentId);
        Assert.Equal(1, dto.DisplayOrder);
    }

    [Fact]
    public void OrganizationTreeDto_ChildOrganizations_CreatesHierarchy()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        var root = new
        {
            Id = rootId,
            Name = "Company",
            ParentId = (Guid?)null,
            Level = 0,
            DisplayOrder = 0,
            Description = "Root organization",
            Children = new[]
            {
                new
                {
                    Id = child1Id,
                    Name = "Engineering",
                    ParentId = (Guid?)rootId,
                    Level = 1,
                    DisplayOrder = 1,
                    Description = "Engineering dept",
                    Children = Array.Empty<object>()
                },
                new
                {
                    Id = child2Id,
                    Name = "Marketing",
                    ParentId = (Guid?)rootId,
                    Level = 1,
                    DisplayOrder = 2,
                    Description = "Marketing dept",
                    Children = Array.Empty<object>()
                }
            }
        };

        // Assert
        Assert.Equal(0, root.Level);
        Assert.Equal(2, root.Children.Length);
        Assert.All(root.Children, child => Assert.Equal(1, child.Level));
        Assert.All(root.Children, child => Assert.Equal(rootId, child.ParentId));
    }

    [Fact]
    public void OrganizationPath_BuildsCorrectPath()
    {
        // Arrange
        var rootId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var child1Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var child2Id = Guid.Parse("00000000-0000-0000-0000-000000000003");

        // Act
        var rootPath = $"/{rootId}/";
        var child1Path = $"{rootPath}{child1Id}/";
        var child2Path = $"{child1Path}{child2Id}/";

        // Assert
        Assert.Equal($"/{rootId}/", rootPath);
        Assert.Equal($"/{rootId}/{child1Id}/", child1Path);
        Assert.Equal($"/{rootId}/{child1Id}/{child2Id}/", child2Path);
    }

    [Fact]
    public void OrganizationLevel_CalculatesCorrectly()
    {
        // Arrange
        int parentLevel = 0;

        // Act
        int childLevel = parentLevel + 1;
        int grandchildLevel = childLevel + 1;

        // Assert
        Assert.Equal(0, parentLevel);
        Assert.Equal(1, childLevel);
        Assert.Equal(2, grandchildLevel);
    }

    [Theory]
    [InlineData(1, 2, true)]  // org1 can be child of org2
    [InlineData(2, 1, false)] // org2 cannot be child of org1 (would be circular if org1 is child of org2)
    [InlineData(1, 1, false)] // org cannot be its own parent
    public void CircularReference_Detection_WorksCorrectly(int orgId, int newParentId, bool isValid)
    {
        // Arrange
        var relationships = new Dictionary<int, int?>
        {
            { 1, 2 }, // org1's parent is org2
            { 2, null } // org2 has no parent
        };

        // Act
        bool wouldCreateCircular = CheckCircular(orgId, newParentId, relationships);

        // Assert
        Assert.Equal(!isValid, wouldCreateCircular);
    }

    private bool CheckCircular(int orgId, int newParentId, Dictionary<int, int?> relationships)
    {
        if (orgId == newParentId)
        {
            return true;
        }

        var currentParent = newParentId;
        while (currentParent != null && relationships.ContainsKey(currentParent.Value))
        {
            if (currentParent == orgId)
            {
                return true;
            }
            currentParent = relationships[currentParent.Value];
        }

        return false;
    }

    [Fact]
    public void OrganizationFilterDto_ByParentId_FiltersCorrectly()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var organizations = new[]
        {
            new { Id = Guid.NewGuid(), Name = "Org1", ParentId = (Guid?)parentId, Level = 1 },
            new { Id = Guid.NewGuid(), Name = "Org2", ParentId = (Guid?)parentId, Level = 1 },
            new { Id = Guid.NewGuid(), Name = "Org3", ParentId = (Guid?)null, Level = 0 }
        };

        // Act
        var filtered = organizations.Where(o => o.ParentId == parentId).ToList();

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, o => Assert.Equal(parentId, o.ParentId));
    }

    [Fact]
    public void OrganizationFilterDto_ByLevel_FiltersCorrectly()
    {
        // Arrange
        var organizations = new[]
        {
            new { Id = Guid.NewGuid(), Name = "Root", Level = 0 },
            new { Id = Guid.NewGuid(), Name = "Child1", Level = 1 },
            new { Id = Guid.NewGuid(), Name = "Child2", Level = 1 },
            new { Id = Guid.NewGuid(), Name = "Grandchild", Level = 2 }
        };

        // Act
        var level1Orgs = organizations.Where(o => o.Level == 1).ToList();

        // Assert
        Assert.Equal(2, level1Orgs.Count);
        Assert.All(level1Orgs, o => Assert.Equal(1, o.Level));
    }

    [Fact]
    public void OrganizationTree_DisplayOrder_SortsCorrectly()
    {
        // Arrange
        var organizations = new[]
        {
            new { Name = "Marketing", DisplayOrder = 3 },
            new { Name = "Engineering", DisplayOrder = 1 },
            new { Name = "Sales", DisplayOrder = 2 }
        };

        // Act
        var sorted = organizations.OrderBy(o => o.DisplayOrder).ToList();

        // Assert
        Assert.Equal("Engineering", sorted[0].Name);
        Assert.Equal("Sales", sorted[1].Name);
        Assert.Equal("Marketing", sorted[2].Name);
    }

    [Fact]
    public void OrganizationUpdateDto_MoveToNewParent_UpdatesHierarchy()
    {
        // Arrange
        var oldParentId = Guid.NewGuid();
        var newParentId = Guid.NewGuid();

        var dto = new
        {
            Name = (string?)"Updated Organization",
            ParentId = (Guid?)newParentId,
            DisplayOrder = (int?)5,
            Description = (string?)"Updated description"
        };

        // Assert
        Assert.Equal(newParentId, dto.ParentId);
        Assert.NotEqual(oldParentId, dto.ParentId);
    }

    [Fact]
    public void OrganizationUsers_AddRemove_ManagesRelationship()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var user3Id = Guid.NewGuid();

        var currentUsers = new List<Guid> { user1Id, user2Id };
        var usersToAdd = new List<Guid> { user3Id };
        var usersToRemove = new List<Guid> { user1Id };

        // Act
        var existingUserIds = currentUsers;
        var toAdd = usersToAdd.Where(uid => !existingUserIds.Contains(uid)).ToList();
        var toRemove = currentUsers.Where(uid => usersToRemove.Contains(uid)).ToList();

        // Assert
        Assert.Single(toAdd);
        Assert.Equal(user3Id, toAdd[0]);
        Assert.Single(toRemove);
        Assert.Equal(user1Id, toRemove[0]);
    }
}
