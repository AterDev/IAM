using CommonMod.Models.SystemSettingDtos;
using Entity;
using EntityFramework.DBProvider;

namespace CommonMod.Managers;

/// <summary>
/// Manager for system setting operations
/// </summary>
public class SystemSettingManager(DefaultDbContext dbContext, ILogger<SystemSettingManager> logger)
    : ManagerBase<DefaultDbContext, SystemSetting>(dbContext, logger)
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

        return await ToPageAsync<SystemSettingFilterDto, SystemSettingItemDto>(filter);
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
        if (await ExistAsync(q => q.Key == dto.Key))
        {
            ErrorMsg = "Setting key already exists";
            return null;
        }

        var entity = new SystemSetting
        {
            Key = dto.Key,
            Value = dto.Value,
            Description = dto.Description,
            Category = dto.Category,
            IsEditable = dto.IsEditable,
            IsPublic = dto.IsPublic,
        };

        var success = await AddAsync(entity);
        return !success ? null : await GetDetailAsync(entity.Id);
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
            ErrorMsg = "Setting not found";
            return null;
        }

        if (!entity.IsEditable)
        {
            ErrorMsg = "Setting is not editable";
            return null;
        }

        entity.Value = dto.Value;
        entity.Description = dto.Description ?? entity.Description;

        var success = await UpdateAsync(entity);
        return !success ? null : await GetDetailAsync(id);
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
            ErrorMsg = "Setting not found";
            return false;
        }

        if (!entity.IsEditable)
        {
            ErrorMsg = "Setting is not editable and cannot be deleted";
            return false;
        }

        return await DeleteAsync(entity);
    }

    /// <summary>
    /// Get all public settings
    /// </summary>
    /// <returns>List of public settings</returns>
    public async Task<List<SystemSettingItemDto>> GetPublicSettingsAsync()
    {
        return await ToListAsync<SystemSettingItemDto>(q => q.IsPublic);
    }
}
