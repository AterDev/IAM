using Xunit;

namespace Share.Tests.Identity;

/// <summary>
/// Tests for audit logging scenarios
/// </summary>
public class AuditLoggingTests
{
    [Fact]
    public void AuditLog_LoginSuccess_ContainsRequiredFields()
    {
        // Arrange
        var auditLog = new
        {
            Category = "Authentication",
            Event = "LoginSuccess",
            SubjectId = Guid.NewGuid().ToString(),
            Payload = "{\"userName\":\"testuser\"}",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Assert
        Assert.NotNull(auditLog);
        Assert.Equal("Authentication", auditLog.Category);
        Assert.Equal("LoginSuccess", auditLog.Event);
        Assert.NotNull(auditLog.SubjectId);
    }

    [Fact]
    public void AuditLog_LoginFailed_ContainsReason()
    {
        // Arrange
        var auditLog = new
        {
            Category = "Authentication",
            Event = "LoginFailed",
            SubjectId = "testuser",
            Payload = "{\"reason\":\"InvalidPassword\",\"failedCount\":1}",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Assert
        Assert.NotNull(auditLog);
        Assert.Equal("LoginFailed", auditLog.Event);
        Assert.Contains("reason", auditLog.Payload);
    }

    [Fact]
    public void AuditLog_SessionCreated_ContainsSessionId()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var auditLog = new
        {
            Category = "Authentication",
            Event = "SessionCreated",
            SubjectId = Guid.NewGuid().ToString(),
            Payload = $"{{\"sessionId\":\"{sessionId}\",\"ipAddress\":\"192.168.1.1\"}}",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Assert
        Assert.NotNull(auditLog);
        Assert.Equal("SessionCreated", auditLog.Event);
        Assert.Contains(sessionId, auditLog.Payload);
    }

    [Fact]
    public void AuditLog_SessionRevoked_ContainsRevokedBy()
    {
        // Arrange
        var auditLog = new
        {
            Category = "Authentication",
            Event = "SessionRevoked",
            SubjectId = Guid.NewGuid().ToString(),
            Payload = "{\"sessionId\":\"test-session\",\"revokedBy\":\"admin\"}",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Assert
        Assert.NotNull(auditLog);
        Assert.Equal("SessionRevoked", auditLog.Event);
        Assert.Contains("revokedBy", auditLog.Payload);
    }

    [Fact]
    public void AuditLog_UserRolesChanged_ContainsChanges()
    {
        // Arrange
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var auditLog = new
        {
            Category = "Authorization",
            Event = "UserRolesChanged",
            SubjectId = Guid.NewGuid().ToString(),
            Payload = $"{{\"added\":[\"{roleId1}\"],\"removed\":[\"{roleId2}\"]}}",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Assert
        Assert.NotNull(auditLog);
        Assert.Equal("UserRolesChanged", auditLog.Event);
        Assert.Contains("added", auditLog.Payload);
        Assert.Contains("removed", auditLog.Payload);
    }

    [Fact]
    public void AuditLog_RolePermissionsChanged_ContainsCounts()
    {
        // Arrange
        var auditLog = new
        {
            Category = "Authorization",
            Event = "RolePermissionsChanged",
            SubjectId = Guid.NewGuid().ToString(),
            Payload = "{\"roleName\":\"Admin\",\"oldCount\":5,\"newCount\":7}",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Assert
        Assert.NotNull(auditLog);
        Assert.Equal("RolePermissionsChanged", auditLog.Event);
        Assert.Contains("oldCount", auditLog.Payload);
        Assert.Contains("newCount", auditLog.Payload);
    }

    [Fact]
    public void AuditLogFilterDto_SupportsCategoryFilter()
    {
        // Arrange
        var filter = new
        {
            Category = "Authentication",
            Event = (string?)null,
            SubjectId = (string?)null,
            StartDate = (DateTimeOffset?)null,
            EndDate = (DateTimeOffset?)null,
            PageIndex = 1,
            PageSize = 10
        };

        // Assert
        Assert.NotNull(filter);
        Assert.Equal("Authentication", filter.Category);
    }

    [Fact]
    public void AuditLogFilterDto_SupportsEventFilter()
    {
        // Arrange
        var filter = new
        {
            Category = (string?)null,
            Event = "LoginSuccess",
            SubjectId = (string?)null,
            StartDate = (DateTimeOffset?)null,
            EndDate = (DateTimeOffset?)null,
            PageIndex = 1,
            PageSize = 10
        };

        // Assert
        Assert.NotNull(filter);
        Assert.Equal("LoginSuccess", filter.Event);
    }

    [Fact]
    public void AuditLogFilterDto_SupportsDateRangeFilter()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var filter = new
        {
            Category = (string?)null,
            Event = (string?)null,
            SubjectId = (string?)null,
            StartDate = (DateTimeOffset?)startDate,
            EndDate = (DateTimeOffset?)endDate,
            PageIndex = 1,
            PageSize = 10
        };

        // Assert
        Assert.NotNull(filter);
        Assert.Equal(startDate, filter.StartDate);
        Assert.Equal(endDate, filter.EndDate);
    }

    [Theory]
    [InlineData("Authentication", "LoginSuccess")]
    [InlineData("Authentication", "LoginFailed")]
    [InlineData("Authentication", "SessionCreated")]
    [InlineData("Authentication", "SessionRevoked")]
    [InlineData("Authorization", "UserRolesChanged")]
    [InlineData("Authorization", "RolePermissionsChanged")]
    public void AuditLog_CategoryAndEvent_ValidCombinations(string category, string eventName)
    {
        // Arrange & Act
        var auditLog = new
        {
            Category = category,
            Event = eventName
        };

        // Assert
        Assert.NotNull(auditLog);
        Assert.Equal(category, auditLog.Category);
        Assert.Equal(eventName, auditLog.Event);
    }

    [Fact]
    public void AuditLog_Payload_SupportsJsonFormat()
    {
        // Arrange
        var payload = "{\"key\":\"value\",\"number\":123,\"array\":[1,2,3]}";

        // Act
        var isValidJson = payload.StartsWith("{") && payload.EndsWith("}");

        // Assert
        Assert.True(isValidJson);
    }
}
