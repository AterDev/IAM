using Share.Exceptions;
using Perigon.PostgreSQL.RawSql;
using Perigon.AspNetCore.Services;
using UserCenterMod.Models;

namespace UserCenterMod.Managers;

/// <summary>
/// Manages users' daily proxy traffic usage.
/// </summary>
public class ProxyUsageManager(
    AppDbFactory dbContextFactory,
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
        var result = await _dbContext.TransactionAsync(async ct =>
        {
            // Serialize updates for a user, including creation of the first row for the day.
            await _dbContext.SqlCommand($"SELECT pg_advisory_xact_lock(hashtextextended({userId.ToString("N")}, 0))")
                .ExecuteAsync(ct);

            var now = DateTimeOffset.UtcNow;
            var dailyUsage = _dbSet.Where(item => item.UserId == userId && item.Date == today);
            var entity = await dailyUsage.FirstOrDefaultAsync(ct);
            long total;
            if (entity is null)
            {
                await _dbSet.InsertAsync(new ProxyUsage { UserId = userId, Date = today, Usage = usage }, ct);
                total = usage;
            }
            else
            {
                total = checked(entity.Usage + usage);
                await dailyUsage.ExecuteUpdateAsync(update => update
                    .Set(item => item.Usage, total)
                    .Set(item => item.UpdatedTime, now), cancellationToken: ct);
            }

            var definition = await _dbContext.UserEntitlementDefinitions
                .Where(item => item.EntitlementCode == HttpProxyEntitlementCode).FirstOrDefaultAsync(ct);
            var entitlementUpdated = false;
            if (definition is not null)
            {
                var count = await _dbContext.UserEntitlements
                    .Where(item => item.UserId == userId
                        && item.EntitlementDefinitionId == definition.Id
                        && item.StartDate <= now
                        && (item.ExpirationDate == null || item.ExpirationDate > now))
                    .ExecuteUpdateAsync(update => update
                        .SetExpression(item => item.CurrentValue, item => item.CurrentValue + usage)
                        .Set(item => item.UpdatedTime, now), cancellationToken: ct);
                entitlementUpdated = count > 0;
            }
            return (total, entitlementUpdated);
        }, cancellationToken);

        if (result.entitlementUpdated)
        {
            await _cacheService.RemoveAsync($"user-center:entitlements:{userId:N}");
        }
        return result.total;
    }

    public async Task<PageList<ProxyUsageItemDto>> GetProxyUsagePageAsync(
        Guid userId,
        ProxyUsageFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(item => item.UserId == userId);
        var count = await query.CountAsync(cancellationToken);
        var usage = await query.OrderByDescending(item => item.Date)
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PageList<ProxyUsageItemDto>
        {
            Count = count,
            PageIndex = filter.PageIndex,
            Data = usage.Select(item => new ProxyUsageItemDto
            {
                Id = item.Id,
                UserId = item.UserId,
                Date = item.Date,
                Usage = item.Usage,
                CreatedTime = item.CreatedTime,
                UpdatedTime = item.UpdatedTime,
            }).ToList(),
        };
    }
}
