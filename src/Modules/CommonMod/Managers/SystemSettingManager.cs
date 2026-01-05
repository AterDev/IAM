using CommonMod.Models.SystemSettingDtos;
using EntityFramework.AppDbFactory;
using Share.Exceptions;
using Microsoft.AspNetCore.Http;
using Mapster;

namespace CommonMod.Managers;

/// <summary>
/// Manager for system setting operations
/// </summary>
public class SystemSettingManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<SystemSettingManager> logger
) : ManagerBase<DefaultDbContext, SystemSetting>(dbContextFactory, userContext, logger)
{
    /// <summary>
    /// Get paged system settings
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of system settings</returns>
    public async Task<PageList<SystemSettingItemDto>> GetPageAsync(SystemSettingFilterDto filter)
    {
        Queryable = Queryable
            .WhereNotNull(filter.Key != null, q => q.Key.Contains(filter.Key!))
            .WhereNotNull(filter.Category != null, q => q.Category == filter.Category)
            .WhereNotNull(filter.IsPublic != null, q => q.IsPublic == filter.IsPublic)
            .WhereNotNull(filter.IsEditable != null, q => q.IsEditable == filter.IsEditable);

        return await PageListAsync<SystemSettingFilterDto, SystemSettingItemDto>(filter);
    }

    /// <summary>
    /// Check if user has permission to access system setting
    /// </summary>
    /// <param name="id">System setting id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        // System settings are accessible by all authenticated users for now
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Get system setting detail by id
    /// </summary>
    /// <param name="id">System setting id</param>
    /// <returns>System setting detail or null</returns>
    public async Task<SystemSettingDetailDto?> GetDetailAsync(Guid id)
    {
        return await FindAsync<SystemSettingDetailDto>(q => q.Id == id);
    }

    /// <summary>
    /// Get system setting by key
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <returns>System setting detail or null</returns>
    public async Task<SystemSettingDetailDto?> GetByKeyAsync(string key)
    {
        return await FindAsync<SystemSettingDetailDto>(q => q.Key == key);
    }

    /// <summary>
    /// Add new system setting
    /// </summary>
    /// <param name="dto">System setting add DTO</param>
    /// <returns>Created system setting detail or null</returns>
    public async Task<SystemSettingDetailDto?> AddAsync(SystemSettingAddDto dto)
    {
        // Check if key already exists
        if (await _dbSet.AnyAsync(q => q.Key == dto.Key))
        {
            throw new BusinessException("SettingKeyExists", StatusCodes.Status400BadRequest);
        }

        var entity = dto.MapTo<SystemSetting>();
        await InsertAsync(entity);
        return await GetDetailAsync(entity.Id);
    }

    /// <summary>
    /// Update system setting
    /// </summary>
    /// <param name="id">System setting id</param>
    /// <param name="dto">System setting update DTO</param>
    /// <returns>Updated system setting detail or null</returns>
    public async Task<SystemSettingDetailDto?> UpdateAsync(Guid id, SystemSettingUpdateDto dto)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException("SettingNotFound", StatusCodes.Status404NotFound);
        }

        if (!entity.IsEditable)
        {
            throw new BusinessException("SettingNotEditable", StatusCodes.Status400BadRequest);
        }

        await UpdateAsync<SystemSettingUpdateDto>(id, dto);
        return await GetDetailAsync(id);
    }

    /// <summary>
    /// Delete system setting
    /// </summary>
    /// <param name="id">System setting id</param>
    /// <returns>True if successful</returns>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException("SettingNotFound", StatusCodes.Status404NotFound);
        }

        if (!entity.IsEditable)
        {
            throw new BusinessException("SettingNotDeletable", StatusCodes.Status400BadRequest);
        }

        await DeleteOrUpdateAsync([id], softDelete: true);
        return true;
    }

    /// <summary>
    /// Get all public settings
    /// </summary>
    /// <returns>List of public settings</returns>
    public async Task<List<SystemSettingItemDto>> GetPublicSettingsAsync()
    {
        return await ListAsync<SystemSettingItemDto>(q => q.IsPublic);
    }
}
