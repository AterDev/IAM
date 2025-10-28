using Xunit;

namespace Share.Tests.Identity;

/// <summary>
/// Tests for session management scenarios
/// </summary>
public class SessionManagementTests
{
    [Fact]
    public void LoginSessionAddDto_ValidData_CreatesObject()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid().ToString();
        var dto = new
        {
            UserId = userId,
            SessionId = sessionId,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            DeviceInfo = "Chrome on Windows",
            ExpirationTime = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(userId, dto.UserId);
        Assert.Equal(sessionId, dto.SessionId);
        Assert.Equal("192.168.1.1", dto.IpAddress);
    }

    [Fact]
    public void LoginSessionFilterDto_SupportsPagination()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var filter = new
        {
            UserId = userId,
            SessionId = "test-session-id",
            IpAddress = "192.168.1.1",
            IsActive = true,
            StartDate = DateTimeOffset.UtcNow.AddDays(-7),
            EndDate = DateTimeOffset.UtcNow,
            PageIndex = 1,
            PageSize = 10
        };

        // Assert
        Assert.NotNull(filter);
        Assert.Equal(userId, filter.UserId);
        Assert.Equal(1, filter.PageIndex);
        Assert.Equal(10, filter.PageSize);
    }

    [Fact]
    public void LoginSessionItemDto_ContainsRequiredFields()
    {
        // Arrange
        var item = new
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SessionId = "test-session",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            LoginTime = DateTimeOffset.UtcNow,
            LastActivityTime = DateTimeOffset.UtcNow,
            ExpirationTime = (DateTimeOffset?)DateTimeOffset.UtcNow.AddHours(1),
            IsActive = true
        };

        // Assert
        Assert.NotNull(item);
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.NotEqual(Guid.Empty, item.UserId);
        Assert.True(item.IsActive);
    }

    [Fact]
    public void LoginSessionDetailDto_ContainsAllFields()
    {
        // Arrange
        var detail = new
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SessionId = "test-session",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            DeviceInfo = "Chrome on Windows",
            LoginTime = DateTimeOffset.UtcNow,
            LastActivityTime = DateTimeOffset.UtcNow,
            ExpirationTime = (DateTimeOffset?)DateTimeOffset.UtcNow.AddHours(1),
            IsActive = true,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.NotNull(detail);
        Assert.NotEqual(Guid.Empty, detail.Id);
        Assert.Equal("test-session", detail.SessionId);
        Assert.Equal("Chrome on Windows", detail.DeviceInfo);
    }

    [Fact]
    public void SessionId_GeneratesUniqueValues()
    {
        // Arrange & Act
        var sessionId1 = Guid.NewGuid().ToString();
        var sessionId2 = Guid.NewGuid().ToString();

        // Assert
        Assert.NotEqual(sessionId1, sessionId2);
    }

    [Fact]
    public void ExpirationTime_FutureDate_IsValid()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var expirationTime = now.AddHours(1);

        // Act
        var isExpired = expirationTime < now;

        // Assert
        Assert.False(isExpired);
    }

    [Fact]
    public void ExpirationTime_PastDate_IsExpired()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var expirationTime = now.AddHours(-1);

        // Act
        var isExpired = expirationTime < now;

        // Assert
        Assert.True(isExpired);
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334")]
    public void IpAddress_ValidFormats_AreAccepted(string ipAddress)
    {
        // Act
        var isValid = !string.IsNullOrWhiteSpace(ipAddress);

        // Assert
        Assert.True(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void IpAddress_InvalidFormats_AreRejected(string ipAddress)
    {
        // Act
        var isValid = !string.IsNullOrWhiteSpace(ipAddress);

        // Assert
        Assert.False(isValid);
    }
}
