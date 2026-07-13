using Share.Exceptions;
using UserCenterMod.Models;

namespace UserCenterMod.Managers;

/// <summary>
/// Manages entitlement definitions available for user assignment.
/// </summary>
public class UserEntitlementDefinitionManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<UserEntitlementDefinitionManager> logger
) : ManagerBase<DefaultDbContext, UserEntitlementDefinition>(dbContextFactory, userContext, logger)
{
    public override Task<bool> HasPermissionAsync(Guid id) => Task.FromResult(_userContext.IsAdmin);

    public async Task<PageList<UserEntitlementDefinitionItemDto>> GetPageAsync(
        UserEntitlementDefinitionFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserEntitlementDefinitions.AsNoTracking()
            .WhereNotNull(filter.Keyword, item =>
                item.DisplayName.Contains(filter.Keyword!) || item.EntitlementCode.Contains(filter.Keyword!))
            .OrderBy(item => item.EntitlementCode)
            .Select(ToItem());

        var count = await query.CountAsync(cancellationToken);
        var data = await query.Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PageList<UserEntitlementDefinitionItemDto>
        {
            Count = count,
            Data = data,
            PageIndex = filter.PageIndex,
        };
    }

    public Task<UserEntitlementDefinitionItemDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.UserEntitlementDefinitions.AsNoTracking().Where(item => item.Id == id)
            .Select(ToItem()).FirstOrDefaultAsync(cancellationToken);

    public async Task<UserEntitlementDefinitionItemDto> AddAsync(
        UserEntitlementDefinitionUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        var values = Normalize(dto);
        if (await _dbSet.AnyAsync(item => item.EntitlementCode == values.Code, cancellationToken))
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var entity = new UserEntitlementDefinition
        {
            DisplayName = values.DisplayName,
            Description = values.Description,
            EntitlementCode = values.Code,
            EntitlementType = dto.EntitlementType,
            Unit = values.Unit,
        };
        await _dbSet.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetDetailAsync(entity.Id, cancellationToken))!;
    }

    public async Task<UserEntitlementDefinitionItemDto> UpdateAsync(
        Guid id,
        UserEntitlementDefinitionUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new BusinessException("NotFound", StatusCodes.Status404NotFound);
        var values = Normalize(dto);
        if (await _dbSet.AnyAsync(item => item.Id != id && item.EntitlementCode == values.Code, cancellationToken))
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        entity.DisplayName = values.DisplayName;
        entity.Description = values.Description;
        entity.EntitlementCode = values.Code;
        entity.EntitlementType = dto.EntitlementType;
        entity.Unit = values.Unit;
        entity.UpdatedTime = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetDetailAsync(id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new BusinessException("NotFound", StatusCodes.Status404NotFound);
        if (await _dbContext.UserEntitlements.AnyAsync(item => item.EntitlementDefinitionId == id, cancellationToken))
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        _dbSet.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Expression<Func<UserEntitlementDefinition, UserEntitlementDefinitionItemDto>> ToItem() => item => new()
    {
        Id = item.Id, DisplayName = item.DisplayName, Description = item.Description,
        EntitlementCode = item.EntitlementCode, EntitlementType = item.EntitlementType,
        Unit = item.Unit, CreatedTime = item.CreatedTime, UpdatedTime = item.UpdatedTime,
    };

    private static (string DisplayName, string? Description, string Code, string Unit) Normalize(UserEntitlementDefinitionUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName) || string.IsNullOrWhiteSpace(dto.EntitlementCode) || string.IsNullOrWhiteSpace(dto.Unit))
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }
        return (dto.DisplayName.Trim(), dto.Description?.Trim(), dto.EntitlementCode.Trim(), dto.Unit.Trim());
    }
}
