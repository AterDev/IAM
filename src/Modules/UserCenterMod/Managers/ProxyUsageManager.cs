using Share.Exceptions;
using Perigon.AspNetCore.Services;
using UserCenterMod.Models;

namespace UserCenterMod.Managers;

/// <summary>
/// Manages users' daily proxy traffic usage.
/// </summary>
public class ProxyUsageManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<ProxyUsageManager> logger,
    CacheService cacheService
) : ManagerBase<DefaultDbContext, ProxyUsage>(dbContextFactory, userContext, logger)
{
    private const string HttpProxyEntitlementCode = "HttpProxy";
    private readonly CacheService _cacheService = cacheService;

    public override Task<bool> HasPermissionAsync(Guid id) => Task.FromResult(true);

    public async Task<long> AddProxyUsageAsync(Guid userId, long usage, CancellationToken cancellationToken = default)
    {
        if (usage <= 0)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var entity = await _dbSet.FirstOrDefaultAsync(
            item => item.UserId == userId && item.Date == today,
            cancellationToken);

        if (entity is null)
        {
            entity = new ProxyUsage { UserId = userId, Date = today, Usage = usage };
            await _dbSet.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.Usage += usage;
            entity.UpdatedTime = DateTimeOffset.UtcNow;
        }

        var now = DateTimeOffset.UtcNow;
        var entitlement = await _dbContext.UserEntitlements
            .Include(item => item.EntitlementDefinition)
            .FirstOrDefaultAsync(
                item => item.UserId == userId
                    && item.EntitlementDefinition!.EntitlementCode == HttpProxyEntitlementCode
                    && item.StartDate <= now
                    && (item.ExpirationDate == null || item.ExpirationDate > now),
                cancellationToken);
        if (entitlement is not null)
        {
            entitlement.CurrentValue += usage;
            entitlement.UpdatedTime = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (entitlement is not null)
        {
            await _cacheService.RemoveAsync($"user-center:entitlements:{userId:N}");
        }
        return entity.Usage;
    }

    public async Task<PageList<ProxyUsageItemDto>> GetProxyUsagePageAsync(
        Guid userId,
        ProxyUsageFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        Queryable = Queryable.Where(item => item.UserId == userId);
        return await PageListAsync<ProxyUsageFilterDto, ProxyUsageItemDto>(filter, cancellationToken);
    }
}
