using AccessMod.Managers;
using AccessMod.Models.ScopeDtos;

namespace ApiService.Controllers;

/// <summary>
/// API scope management controller
/// </summary>
public class ScopesController(
    Share.Localizer localizer,
    ScopeManager manager,
    IUserContext user,
    ILogger<ScopesController> logger
) : RestControllerBase<ScopeManager>(localizer, manager, user, logger)
{
    /// <summary>
    /// Get paged scopes
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of scopes</returns>
    [HttpGet]
    public async Task<ActionResult<PageList<ScopeItemDto>>> GetScopes(
        [FromQuery] ScopeFilterDto filter
    )
    {
        var result = await _manager.GetPageAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get scope detail by id
    /// </summary>
    /// <param name="id">Scope id</param>
    /// <returns>Scope detail</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ScopeDetailDto>> GetDetail(Guid id)
    {
        var result = await _manager.GetDetailAsync(id);
        return result == null ? NotFound("Scope not found") : Ok(result);
    }

    /// <summary>
    /// Create new scope
    /// </summary>
    /// <param name="dto">Scope data</param>
    /// <returns>Created scope detail</returns>
    [HttpPost]
    public async Task<ActionResult<ScopeDetailDto>> CreateScope([FromBody] ScopeAddDto dto)
    {
        var result = await _manager.AddAsync(dto);
        return result == null
            ? BadRequest(_manager.ErrorMsg)
            : CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update scope
    /// </summary>
    /// <param name="id">Scope id</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated scope detail</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ScopeDetailDto>> UpdateScope(
        Guid id,
        [FromBody] ScopeUpdateDto dto
    )
    {
        var result = await _manager.UpdateAsync(id, dto);
        return result == null ? BadRequest(_manager.ErrorMsg) : Ok(result);
    }

    /// <summary>
    /// Delete scope
    /// </summary>
    /// <param name="id">Scope id</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteScope(Guid id)
    {
        var success = await _manager.DeleteAsync(id);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }
}
