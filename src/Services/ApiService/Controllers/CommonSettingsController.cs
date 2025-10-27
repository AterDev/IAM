using CommonMod.Managers;
using CommonMod.Models.SystemSettingDtos;
using Microsoft.AspNetCore.Authorization;

namespace ApiService.Controllers;

/// <summary>
/// Common settings controller
/// </summary>
public class CommonSettingsController(
    Share.Localizer localizer,
    SystemSettingManager manager,
    IUserContext user,
    ILogger<CommonSettingsController> logger
) : RestControllerBase<SystemSettingManager>(localizer, manager, user, logger)
{
    /// <summary>
    /// Get paged system settings
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of settings</returns>
    [HttpGet]
    public async Task<ActionResult<PageList<SystemSettingItemDto>>> GetSettings(
        [FromQuery] SystemSettingFilterDto filter
    )
    {
        var result = await _manager.GetPageAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get public settings (no authentication required)
    /// </summary>
    /// <returns>List of public settings</returns>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SystemSettingItemDto>>> GetPublicSettings()
    {
        var result = await _manager.GetPublicSettingsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get system setting detail by id
    /// </summary>
    /// <param name="id">Setting id</param>
    /// <returns>Setting detail</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<SystemSettingDetailDto>> GetDetail(Guid id)
    {
        var result = await _manager.GetDetailAsync(id);
        return result == null
            ? (ActionResult<SystemSettingDetailDto>)NotFound("Setting not found")
            : (ActionResult<SystemSettingDetailDto>)Ok(result);
    }

    /// <summary>
    /// Get system setting by key
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <returns>Setting detail</returns>
    [HttpGet("key/{key}")]
    public async Task<ActionResult<SystemSettingDetailDto>> GetByKey(string key)
    {
        var result = await _manager.GetByKeyAsync(key);
        return result == null
            ? (ActionResult<SystemSettingDetailDto>)NotFound("Setting not found")
            : (ActionResult<SystemSettingDetailDto>)Ok(result);
    }

    /// <summary>
    /// Create new system setting
    /// </summary>
    /// <param name="dto">Setting data</param>
    /// <returns>Created setting detail</returns>
    [HttpPost]
    public async Task<ActionResult<SystemSettingDetailDto>> CreateSetting(
        [FromBody] SystemSettingAddDto dto
    )
    {
        var result = await _manager.AddAsync(dto);
        return result == null
            ? (ActionResult<SystemSettingDetailDto>)BadRequest(_manager.ErrorMsg)
            : (ActionResult<SystemSettingDetailDto>)
                CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update system setting
    /// </summary>
    /// <param name="id">Setting id</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated setting detail</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<SystemSettingDetailDto>> UpdateSetting(
        Guid id,
        [FromBody] SystemSettingUpdateDto dto
    )
    {
        var result = await _manager.UpdateAsync(id, dto);
        return result == null
            ? (ActionResult<SystemSettingDetailDto>)BadRequest(_manager.ErrorMsg)
            : (ActionResult<SystemSettingDetailDto>)Ok(result);
    }

    /// <summary>
    /// Delete system setting
    /// </summary>
    /// <param name="id">Setting id</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSetting(Guid id)
    {
        var success = await _manager.DeleteAsync(id);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }
}
