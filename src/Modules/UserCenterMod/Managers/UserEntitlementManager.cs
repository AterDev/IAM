using Perigon.AspNetCore.Services;
using Share.Exceptions;
using UserCenterMod.Models;

namespace UserCenterMod.Managers;

/// <summary>
/// Manages entitlement assignments and cached entitlement lookups.
/// </summary>
public class UserEntitlementManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<UserEntitlementManager> logger,
    CacheService cacheService
) : ManagerBase<DefaultDbContext, UserEntitlement>(dbContextFactory, userContext, logger)
{
    private const int EntitlementCacheSeconds = 2 * 60;
    private readonly CacheService _cacheService = cacheService;

    public override Task<bool> HasPermissionAsync(Guid id) => Task.FromResult(_userContext.IsAdmin);

    public async Task<PageList<UserEntitlementDetailDto>> GetPageAsync(
        UserEntitlementFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        if (filter.UserId == Guid.Empty)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var query = _dbContext.UserEntitlements.AsNoTracking().Where(item => item.UserId == filter.UserId)
            .OrderBy(item => item.EntitlementDefinition!.EntitlementCode).Select(ToDetail());
        var count = await query.CountAsync(cancellationToken);
        var data = await query.Skip((filter.PageIndex - 1) * filter.PageSize).Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PageList<UserEntitlementDetailDto> { Count = count, Data = data, PageIndex = filter.PageIndex };
    }

    public Task<UserEntitlementDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.UserEntitlements.AsNoTracking().Where(item => item.Id == id).Select(ToDetail())
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<UserEntitlementDetailDto> AddAsync(
        Guid userId,
        UserEntitlementAddDto dto,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || dto.EntitlementDefinitionId == Guid.Empty || dto.StartDate == default)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }
        if (dto.ExpirationDate is { } expirationDate && expirationDate <= dto.StartDate)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }
        if (!await _dbContext.Users.AnyAsync(item => item.Id == userId, cancellationToken)
            || !await _dbContext.UserEntitlementDefinitions.AnyAsync(item => item.Id == dto.EntitlementDefinitionId, cancellationToken)
            || await _dbSet.AnyAsync(item => item.UserId == userId && item.EntitlementDefinitionId == dto.EntitlementDefinitionId, cancellationToken))
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var entity = new UserEntitlement
        {
            UserId = userId,
            EntitlementDefinitionId = dto.EntitlementDefinitionId,
            ValueLimit = dto.ValueLimit,
            ExpirationDate = dto.ExpirationDate,
            StartDate = dto.StartDate,
        };
        await _dbSet.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync(userId);
        return (await GetDetailAsync(entity.Id, cancellationToken))!;
    }

    public async Task<UserEntitlementDetailDto> UpdateAsync(
        Guid id,
        UserEntitlementUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new BusinessException("NotFound", StatusCodes.Status404NotFound);
        if (dto.StartDate == default || dto.ExpirationDate is { } expirationDate && expirationDate <= dto.StartDate)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }
        entity.ValueLimit = dto.ValueLimit;
        entity.ExpirationDate = dto.ExpirationDate;
        entity.StartDate = dto.StartDate;
        entity.UpdatedTime = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync(entity.UserId);
        return (await GetDetailAsync(id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new BusinessException("NotFound", StatusCodes.Status404NotFound);
        _dbSet.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync(entity.UserId);
    }

    public async Task<UserEntitlementDetailDto?> GetActiveEntitlementAsync(
        Guid userId,
        string entitlementCode,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(entitlementCode)) return null;
        var code = entitlementCode.Trim();
        var entitlements = await _cacheService.GetOrCreateWithExpirationAsync(
            GetCacheKey(userId),
            async ct => await GetActiveEntitlementsAsync(userId, ct),
            expiration: EntitlementCacheSeconds,
            cancellation: cancellationToken);
        return entitlements.FirstOrDefault(item => item.EntitlementCode.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    private Task<List<UserEntitlementDetailDto>> GetActiveEntitlementsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return _dbContext.UserEntitlements.AsNoTracking()
            .Where(item => item.UserId == userId && item.StartDate <= now
                && (item.ExpirationDate == null || item.ExpirationDate > now))
            .Select(ToDetail()).ToListAsync(cancellationToken);
    }

    private Task InvalidateCacheAsync(Guid userId) => _cacheService.RemoveAsync(GetCacheKey(userId));
    private static string GetCacheKey(Guid userId) => $"user-center:entitlements:{userId:N}";

    private static Expression<Func<UserEntitlement, UserEntitlementDetailDto>> ToDetail() => item => new()
    {
        Id = item.Id, UserId = item.UserId, EntitlementDefinitionId = item.EntitlementDefinitionId,
        DisplayName = item.EntitlementDefinition!.DisplayName,
        Description = item.EntitlementDefinition.Description,
        EntitlementCode = item.EntitlementDefinition.EntitlementCode,
        EntitlementType = item.EntitlementDefinition.EntitlementType,
        Unit = item.EntitlementDefinition.Unit,
        ValueLimit = item.ValueLimit, CurrentValue = item.CurrentValue,
        ExpirationDate = item.ExpirationDate, StartDate = item.StartDate,
        CreatedTime = item.CreatedTime, UpdatedTime = item.UpdatedTime,
    };
}
