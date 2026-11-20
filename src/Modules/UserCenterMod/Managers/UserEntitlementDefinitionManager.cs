using Share.Exceptions;
using UserCenterMod.Models;

namespace UserCenterMod.Managers;

/// <summary>
/// Manages entitlement definitions available for user assignment.
/// </summary>
public class UserEntitlementDefinitionManager(
    AppDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<UserEntitlementDefinitionManager> logger
) : ManagerBase<DefaultDbContext, UserEntitlementDefinition>(dbContextFactory, userContext, logger)
{
    public override Task<bool> HasPermissionAsync(Guid id) => Task.FromResult(_userContext.IsAdmin);

    public async Task<PageList<UserEntitlementDefinitionItemDto>> GetPageAsync(
        UserEntitlementDefinitionFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserEntitlementDefinitions
            .WhereNotNull(filter.Keyword, item =>
                item.DisplayName.Contains(filter.Keyword!) || item.EntitlementCode.Contains(filter.Keyword!))
            .OrderBy(item => item.EntitlementCode);

        var count = await query.CountAsync(cancellationToken);
        var data = await query.Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PageList<UserEntitlementDefinitionItemDto>
        {
            Count = count,
            Data = data.Select(ToItem).ToList(),
            PageIndex = filter.PageIndex,
        };
    }

    public async Task<UserEntitlementDefinitionItemDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.Where(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : ToItem(entity);
    }

    public async Task<UserEntitlementDefinitionItemDto> AddAsync(
        UserEntitlementDefinitionUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        var values = Normalize(dto);
        if (await _dbSet.Where(item => item.EntitlementCode == values.Code).AnyAsync(cancellationToken))
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
        await _dbSet.InsertAsync(entity, cancellationToken);
        return (await GetDetailAsync(entity.Id, cancellationToken))!;
    }

    public async Task<UserEntitlementDefinitionItemDto> UpdateAsync(
        Guid id,
        UserEntitlementDefinitionUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.Where(item => item.Id == id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException("NotFound", StatusCodes.Status404NotFound);
        var values = Normalize(dto);
        if (await _dbSet.Where(item => item.Id != id && item.EntitlementCode == values.Code).AnyAsync(cancellationToken))
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var updatedAt = DateTimeOffset.UtcNow;
        await _dbSet.Where(item => item.Id == entity.Id)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.DisplayName, values.DisplayName)
                .Set(item => item.Description, values.Description)
                .Set(item => item.EntitlementCode, values.Code)
                .Set(item => item.EntitlementType, dto.EntitlementType)
                .Set(item => item.Unit, values.Unit)
                .Set(item => item.UpdatedTime, updatedAt), cancellationToken: cancellationToken);
        return (await GetDetailAsync(id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.Where(item => item.Id == id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException("NotFound", StatusCodes.Status404NotFound);
        if (await _dbContext.UserEntitlements.Where(item => item.EntitlementDefinitionId == id).AnyAsync(cancellationToken))
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        await _dbSet.Where(item => item.Id == entity.Id).ExecuteDeleteAsync(cancellationToken: cancellationToken);
    }

    private static UserEntitlementDefinitionItemDto ToItem(UserEntitlementDefinition item) => new()
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
