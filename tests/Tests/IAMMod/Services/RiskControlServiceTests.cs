using Tests.IAMMod.Managers;

namespace Tests.IAMMod.Services;

public class RiskControlServiceTests
{
    [Fact]
    public async Task EvaluateLoginRiskAsync_WithUnfamiliarIpAndUserAgent_ReturnsStepUpRecommendation()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(EvaluateLoginRiskAsync_WithUnfamiliarIpAndUserAgent_ReturnsStepUpRecommendation));
        var user = new User
        {
            UserName = "alice",
            NormalizedUserName = "ALICE",
            Email = "alice@example.com",
            NormalizedEmail = "ALICE@EXAMPLE.COM",
            PasswordHash = "hash",
            PasswordSalt = "salt",
        };

        dbContext.Users.Add(user);
        dbContext.LoginSessions.Add(new LoginSession
        {
            UserId = user.Id,
            SessionId = Guid.NewGuid().ToString("N"),
            IpAddress = "10.0.0.1",
            UserAgent = "Chrome",
            DeviceInfo = "Chrome",
            LoginTime = DateTimeOffset.UtcNow.AddMinutes(-10),
            LastActivityTime = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpirationTime = DateTimeOffset.UtcNow.AddHours(1),
            IsActive = true,
        });
        await dbContext.SaveChangesAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new RiskControlService(
            dbContext,
            cache,
            Options.Create(new RiskControlOption()),
            NullLogger<RiskControlService>.Instance);

        var result = await service.EvaluateLoginRiskAsync(user.Id, "10.0.0.2", "Firefox");

        Assert.False(result.HasKnownIp);
        Assert.False(result.HasKnownUserAgent);
        Assert.True(result.RequiresStepUp);
        Assert.Equal("10.0.0.1", result.LastKnownIpAddress);
        Assert.Equal("Chrome", result.LastKnownUserAgent);
    }

    [Fact]
    public async Task RegisterDeviceCodePoll_WhenThresholdExceeded_ReturnsBlockedAssessment()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(RegisterDeviceCodePoll_WhenThresholdExceeded_ReturnsBlockedAssessment));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new RiskControlService(
            dbContext,
            cache,
            Options.Create(new RiskControlOption
            {
                DeviceCodePollLimit = 2,
                DeviceCodePollWindowSeconds = 60,
            }),
            NullLogger<RiskControlService>.Instance);

        _ = service.RegisterDeviceCodePoll("client-a", "device-code-1");
        _ = service.RegisterDeviceCodePoll("client-a", "device-code-1");
        var blocked = service.RegisterDeviceCodePoll("client-a", "device-code-1");

        Assert.True(blocked.IsBlocked);
        Assert.Equal(3, blocked.AttemptCount);
        Assert.NotNull(blocked.BlockedUntil);
    }
}