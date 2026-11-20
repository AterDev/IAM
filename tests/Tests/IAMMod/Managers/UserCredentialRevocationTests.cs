using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Perigon.AspNetCore.Services;

namespace Tests.IAMMod.Managers;

public class UserCredentialRevocationTests
{
    [Fact]
    public async Task GetPageAsync_ReportsLockoutEndSeparatelyFromLockoutPolicy()
    {
        await using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var user = CreateUser("policy-enabled");
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        using var provider = CreateCacheProvider();

        var page = await CreateManager(dbContext, CreateValidator(dbContext, provider))
            .GetPageAsync(new global::IAMMod.Models.UserDtos.UserFilterDto());

        var item = Assert.Single(page.Data);
        Assert.True(item.LockoutEnabled);
        Assert.InRange(
            (item.LockoutEnd!.Value - user.LockoutEnd!.Value).Duration(),
            TimeSpan.Zero,
            TimeSpan.FromMicroseconds(1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangePasswordAsync_RevokesAllCredentialsIncludingSessionlessTokens(bool hasSessions)
    {
        await using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var user = CreateUser("alice");
        var otherUser = CreateUser("bob");
        dbContext.Users.AddRange(user, otherUser);
        var tokens = new[]
        {
            CreateToken(user.Id, OAuthConst.TokenTypes.AccessToken),
            CreateToken(user.Id, OAuthConst.TokenTypes.RefreshToken),
            CreateToken(user.Id, OAuthConst.TokenTypes.AuthorizationCode),
            CreateToken(user.Id, OAuthConst.TokenTypes.DeviceCode),
        };
        var otherToken = CreateToken(otherUser.Id, OAuthConst.TokenTypes.RefreshToken);
        dbContext.Tokens.AddRange(tokens);
        dbContext.Tokens.Add(otherToken);
        var sessions = hasSessions
            ? new[] { CreateSession(user.Id, "browser"), CreateSession(user.Id, "phone") }
            : Array.Empty<LoginSession>();
        var otherSession = CreateSession(otherUser.Id, "other-user");
        dbContext.LoginSessions.AddRange(sessions);
        dbContext.LoginSessions.Add(otherSession);
        await dbContext.SaveChangesAsync();

        using var provider = CreateCacheProvider();
        var validator = CreateValidator(dbContext, provider);
        foreach (var session in sessions)
        {
            Assert.True(await validator.ValidateAsync(user.Id, session.SessionId));
        }

        var manager = CreateManager(dbContext, validator);
        Assert.True(await manager.ChangePasswordAsync(user.Id, "New-password-2026!"));

        var storedUser = await dbContext.Users
            .Where(item => item.Id == user.Id)
            .FirstOrDefaultAsync();
        Assert.NotNull(storedUser);
        Assert.True(HashCrypto.Validate("New-password-2026!", storedUser.PasswordSalt, storedUser.PasswordHash));
        var tokenIds = tokens.Select(token => token.Id).ToArray();
        var storedTokens = await dbContext.Tokens
            .Where(item => tokenIds.Contains(item.Id))
            .ToListAsync();
        Assert.Equal(tokens.Length, storedTokens.Count);
        Assert.All(storedTokens, token => Assert.Equal(OAuthConst.TokenStatuses.Revoked, token.Status));
        foreach (var session in sessions)
        {
            Assert.False(await validator.ValidateAsync(user.Id, session.SessionId));
        }
        Assert.True(await validator.ValidateAsync(otherUser.Id, otherSession.SessionId));
        var storedOtherToken = await dbContext.Tokens
            .Where(item => item.Id == otherToken.Id)
            .FirstOrDefaultAsync();
        Assert.NotNull(storedOtherToken);
        Assert.Equal(OAuthConst.TokenStatuses.Valid, storedOtherToken.Status);
        Assert.False(await dbContext.LoginSessions.AnyAsync(s => s.UserId == user.Id && s.IsActive));
        var userSubjectId = user.Id.ToString();
        Assert.False(await dbContext.Tokens.AnyAsync(t => t.SubjectId == userSubjectId
            && t.Status == OAuthConst.TokenStatuses.Valid));
    }

    [Fact]
    public async Task SetLockoutAsync_LockingRevokesCredentialsAndUnlockingDoesNotRestoreThem()
    {
        await using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var user = CreateUser("locked");
        var session = CreateSession(user.Id, "locked-session");
        var token = CreateToken(user.Id, OAuthConst.TokenTypes.RefreshToken);
        dbContext.Users.Add(user);
        dbContext.LoginSessions.Add(session);
        dbContext.Tokens.Add(token);
        await dbContext.SaveChangesAsync();
        using var provider = CreateCacheProvider();
        var validator = CreateValidator(dbContext, provider);
        Assert.True(await validator.ValidateAsync(user.Id, session.SessionId));
        var manager = CreateManager(dbContext, validator);

        await manager.SetLockoutAsync(user.Id, DateTimeOffset.UtcNow.AddHours(1));
        Assert.False(await validator.ValidateAsync(user.Id, session.SessionId));
        var storedToken = await dbContext.Tokens
            .Where(item => item.Id == token.Id)
            .FirstOrDefaultAsync();
        Assert.NotNull(storedToken);
        Assert.Equal(OAuthConst.TokenStatuses.Revoked, storedToken.Status);

        await manager.SetLockoutAsync(user.Id, null);
        Assert.Null(user.LockoutEnd);
        Assert.False(await validator.ValidateAsync(user.Id, session.SessionId));
        storedToken = await dbContext.Tokens
            .Where(item => item.Id == token.Id)
            .FirstOrDefaultAsync();
        Assert.NotNull(storedToken);
        Assert.Equal(OAuthConst.TokenStatuses.Revoked, storedToken.Status);
    }

    [Fact]
    public async Task SetLockoutAsync_UnlockingDoesNotRevokeActiveCredentials()
    {
        await using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var user = CreateUser("unlocked");
        var session = CreateSession(user.Id, "active-session");
        dbContext.Users.Add(user);
        dbContext.LoginSessions.Add(session);
        await dbContext.SaveChangesAsync();
        using var provider = CreateCacheProvider();
        var validator = CreateValidator(dbContext, provider);

        await CreateManager(dbContext, validator).SetLockoutAsync(user.Id, null);

        Assert.True(await validator.ValidateAsync(user.Id, session.SessionId));
    }

    private static UserManager CreateManager(DefaultDbContext context, SessionValidationService validator)
    {
        return ManagerTestFactory.Create<UserManager, User>(context,
            new Dictionary<string, object?> { ["_sessionValidationService"] = validator });
    }

    private static User CreateUser(string name)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = name,
            NormalizedUserName = name.ToUpperInvariant(),
            Email = $"{name}@example.com",
            NormalizedEmail = $"{name}@example.com".ToUpperInvariant(),
            PasswordSalt = "test-salt",
            PasswordHash = HashCrypto.GeneratePwd("Old-password-2026!", "test-salt"),
        };
    }

    private static Token CreateToken(Guid userId, string type)
    {
        return new Token
        {
            SubjectId = userId.ToString(),
            Type = type,
            Status = OAuthConst.TokenStatuses.Valid,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddHours(1),
        };
    }

    private static LoginSession CreateSession(Guid userId, string sessionId)
    {
        return new LoginSession
        {
            UserId = userId,
            SessionId = sessionId,
            IsActive = true,
            LoginTime = DateTimeOffset.UtcNow,
            LastActivityTime = DateTimeOffset.UtcNow,
            ExpirationTime = DateTimeOffset.UtcNow.AddHours(1),
        };
    }

    private static ServiceProvider CreateCacheProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();
        return services.BuildServiceProvider();
    }

    private static SessionValidationService CreateValidator(DefaultDbContext context, ServiceProvider provider)
    {
        var cache = new CacheService(provider.GetRequiredService<HybridCache>(),
            Options.Create(new CacheOption()),
            Options.Create(new ComponentOption { Cache = CacheType.Memory }));
        return new SessionValidationService(context, cache, Options.Create(new JwtOption
        {
            ExpiredSecond = 3600,
            ValidAudiences = "test",
            Sign = "test-signing-key-for-session-validation",
        }));
    }
}
