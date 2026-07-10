using Entity.UserCenterMod;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Perigon.AspNetCore.Services;
using Tests.IAMMod.Managers;
using UserCenterMod.Managers;
using UserCenterMod.Models;

namespace Tests.UserCenter.Managers;

public class UserCenterManagerTests
{
    private static readonly CacheService CacheService = CreateCacheService();

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsUserId()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(LoginAsync_WithValidCredentials_ReturnsUserId));
        var user = SeedUser(dbContext, "login@example.com", "P@ssw0rd!");
        var manager = CreateManager(dbContext);

        var userId = await manager.LoginAsync(new UserCenterLoginDto
        {
            Email = "  LOGIN@example.com ",
            Password = "P@ssw0rd!",
        });

        Assert.Equal(user.Id, userId);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedBusinessException()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(LoginAsync_WithInvalidPassword_ThrowsUnauthorizedBusinessException));
        SeedUser(dbContext, "login@example.com", "P@ssw0rd!");
        var manager = CreateManager(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.LoginAsync(new UserCenterLoginDto
        {
            Email = "login@example.com",
            Password = "incorrect",
        }));

        Assert.Equal(Localizer.InvalidEmailOrPassword, exception.LanguageKey);
        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCodes);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithCurrentUserToken_ReturnsTokenUser()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ValidateTokenAsync_WithCurrentUserToken_ReturnsTokenUser));
        var user = SeedUser(dbContext, "token@example.com", "P@ssw0rd!");
        var manager = CreateManager(dbContext);

        var result = await manager.ValidateTokenAsync($"Bearer {CreateToken(user, DateTimeOffset.UtcNow)}");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.NormalizedEmail, result.NormalizedEmail);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ValidateTokenAsync_WithExpiredToken_ReturnsNull));
        var user = SeedUser(dbContext, "expired@example.com", "P@ssw0rd!");
        var manager = CreateManager(dbContext);

        var result = await manager.ValidateTokenAsync(CreateToken(user, DateTimeOffset.UtcNow.AddSeconds(-6)));

        Assert.Null(result);
    }

    [Fact]
    public async Task AddProxyUsageAsync_OnSameDate_AccumulatesIntoOneRecord()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(AddProxyUsageAsync_OnSameDate_AccumulatesIntoOneRecord));
        var manager = CreateManager(dbContext);
        var userId = Guid.CreateVersion7();

        var firstUsage = await manager.AddProxyUsageAsync(userId, 128);
        var secondUsage = await manager.AddProxyUsageAsync(userId, 64);

        Assert.Equal(128, firstUsage);
        Assert.Equal(192, secondUsage);

        var usage = await dbContext.ProxyUsages.SingleAsync(q => q.UserId == userId);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now), usage.Date);
        Assert.Equal(192, usage.Usage);
    }

    [Fact]
    public async Task GetProxyUsagePageAsync_ReturnsOnlyCurrentUserUsage()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(GetProxyUsagePageAsync_ReturnsOnlyCurrentUserUsage));
        var manager = CreateManager(dbContext);
        var userId = Guid.CreateVersion7();
        var otherUserId = Guid.CreateVersion7();

        dbContext.ProxyUsages.AddRange(
            new ProxyUsage { UserId = userId, Date = new DateOnly(2026, 7, 9), Usage = 256 },
            new ProxyUsage { UserId = otherUserId, Date = new DateOnly(2026, 7, 9), Usage = 512 }
        );
        await dbContext.SaveChangesAsync();

        var result = await manager.GetProxyUsagePageAsync(userId, new ProxyUsageFilterDto
        {
            PageIndex = 1,
            PageSize = 10,
        });

        var usage = Assert.Single(result.Data);
        Assert.Equal(1, result.Count);
        Assert.Equal(userId, usage.UserId);
        Assert.Equal(256, usage.Usage);
    }

    private static UserCenterManager CreateManager(DefaultDbContext dbContext)
    {
        return ManagerTestFactory.Create<UserCenterManager, ProxyUsage>(
            dbContext,
            new Dictionary<string, object?>
            {
                ["_cacheService"] = CacheService,
            }
        );
    }

    private static CacheService CreateCacheService()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var serviceProvider = services.BuildServiceProvider();

        return new CacheService(
            serviceProvider.GetRequiredService<HybridCache>(),
            Options.Create(new CacheOption()),
            Options.Create(new ComponentOption())
        );
    }

    private static User SeedUser(DefaultDbContext dbContext, string email, string password)
    {
        var salt = HashCrypto.BuildSalt();
        var user = new User
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PasswordSalt = salt,
            PasswordHash = HashCrypto.GeneratePwd(password, salt),
            LockoutEnabled = true,
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();
        return user;
    }

    private static string CreateToken(User user, DateTimeOffset timestamp)
    {
        var payload = $"{user.Id}::{user.Email}::{timestamp.ToUnixTimeSeconds()}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
    }
}
