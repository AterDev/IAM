using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Perigon.AspNetCore.Services;
using Tests.IAMMod.Managers;

namespace Tests.IAMMod.Services;

public class SessionValidationServiceTests
{
    [Fact]
    public async Task ValidateAsync_WhenSessionRecentlySynced_DoesNotWriteDatabaseOnEveryRequest()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ValidateAsync_WhenSessionRecentlySynced_DoesNotWriteDatabaseOnEveryRequest));
        var session = new LoginSession
        {
            UserId = Guid.NewGuid(),
            SessionId = "sid-recent",
            LoginTime = DateTimeOffset.UtcNow.AddMinutes(-10),
            LastActivityTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpirationTime = DateTimeOffset.UtcNow.AddHours(1),
            IsActive = true,
        };
        dbContext.LoginSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var originalLastActivity = session.LastActivityTime;
        var service = CreateService(dbContext);

        var isValid = await service.ValidateAsync(session.UserId, session.SessionId);

        dbContext.ChangeTracker.Clear();
        var storedSession = await dbContext.LoginSessions
            .AsNoTracking()
            .SingleAsync((LoginSession q) => q.SessionId == session.SessionId);

        Assert.True(isValid);
        Assert.Equal(originalLastActivity, storedSession.LastActivityTime);
    }

    [Fact]
    public async Task ValidateAsync_WhenSyncIntervalElapsed_PersistsLatestActivity()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ValidateAsync_WhenSyncIntervalElapsed_PersistsLatestActivity));
        var session = new LoginSession
        {
            UserId = Guid.NewGuid(),
            SessionId = "sid-stale",
            LoginTime = DateTimeOffset.UtcNow.AddHours(-1),
            LastActivityTime = DateTimeOffset.UtcNow.AddMinutes(-10),
            ExpirationTime = DateTimeOffset.UtcNow.AddHours(1),
            IsActive = true,
        };
        dbContext.LoginSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var originalLastActivity = session.LastActivityTime;
        var service = CreateService(dbContext);

        var isValid = await service.ValidateAsync(session.UserId, session.SessionId);

        dbContext.ChangeTracker.Clear();
        var storedSession = await dbContext.LoginSessions
            .AsNoTracking()
            .SingleAsync((LoginSession q) => q.SessionId == session.SessionId);

        Assert.True(isValid);
        Assert.True(storedSession.LastActivityTime > originalLastActivity);
    }

    [Fact]
    public async Task ValidateAsync_WhenCacheIsOverriddenWithInactiveState_RejectsSession()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ValidateAsync_WhenCacheIsOverriddenWithInactiveState_RejectsSession));
        var session = new LoginSession
        {
            UserId = Guid.NewGuid(),
            SessionId = "sid-revoked",
            LoginTime = DateTimeOffset.UtcNow.AddMinutes(-20),
            LastActivityTime = DateTimeOffset.UtcNow.AddMinutes(-2),
            ExpirationTime = DateTimeOffset.UtcNow.AddHours(1),
            IsActive = true,
        };
        dbContext.LoginSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.SetStateAsync(new SessionValidationCacheEntry
        {
            UserId = session.UserId,
            SessionId = session.SessionId,
            IsActive = false,
            LastActivityTime = session.LastActivityTime,
            LastPersistedActivityTime = session.LastActivityTime,
            ExpirationTime = session.ExpirationTime,
        });

        var isValid = await service.ValidateAsync(session.UserId, session.SessionId);

        Assert.False(isValid);
    }

    private static SessionValidationService CreateService(DefaultDbContext dbContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddHybridCache();

        var serviceProvider = services.BuildServiceProvider();
        var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
        var cacheService = new CacheService(
            hybridCache,
            Options.Create(new CacheOption()),
            Options.Create(new ComponentOption { Cache = CacheType.Memory }));

        return new SessionValidationService(
            dbContext,
            cacheService,
            Options.Create(new JwtOption
            {
                ValidAudiences = "tests",
                Sign = "tests-sign",
                ExpiredSecond = 7200,
            }));
    }
}