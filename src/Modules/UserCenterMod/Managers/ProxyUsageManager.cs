using Share.Exceptions;
using UserCenterMod.Models;

namespace UserCenterMod.Managers;

/// <summary>
/// Manages users' daily proxy traffic usage.
/// </summary>
public class ProxyUsageManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<ProxyUsageManager> logger
) : ManagerBase<DefaultDbContext, ProxyUsage>(dbContextFactory, userContext, logger)
{
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

        await _dbContext.SaveChangesAsync(cancellationToken);
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
