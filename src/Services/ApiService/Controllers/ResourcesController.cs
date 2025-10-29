using AccessMod.Managers;
using AccessMod.Models.ResourceDtos;

namespace ApiService.Controllers;

/// <summary>
/// API resource management controller
/// </summary>
public class ResourcesController(
    Share.Localizer localizer,
    ResourceManager manager,
    IUserContext user,
    ILogger<ResourcesController> logger
) : RestControllerBase<ResourceManager>(localizer, manager, user, logger)
{
    /// <summary>
    /// Get paged resources
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of resources</returns>
    [HttpGet]
    public async Task<ActionResult<PageList<ResourceItemDto>>> GetResources(
        [FromQuery] ResourceFilterDto filter
    )
    {
        var result = await _manager.GetPageAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get resource detail by id
    /// </summary>
    /// <param name="id">Resource id</param>
    /// <returns>Resource detail</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResourceDetailDto>> GetDetail(Guid id)
    {
        var result = await _manager.GetDetailAsync(id);
        return result == null ? NotFound("Resource not found") : Ok(result);
    }

    /// <summary>
    /// Create new resource
    /// </summary>
    /// <param name="dto">Resource data</param>
    /// <returns>Created resource detail</returns>
    [HttpPost]
    public async Task<ActionResult<ResourceDetailDto>> CreateResource(
        [FromBody] ResourceAddDto dto
    )
    {
        var result = await _manager.AddAsync(dto);
        return result == null
            ? BadRequest(_manager.ErrorMsg)
            : CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update resource
    /// </summary>
    /// <param name="id">Resource id</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated resource detail</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ResourceDetailDto>> UpdateResource(
        Guid id,
        [FromBody] ResourceUpdateDto dto
    )
    {
        var result = await _manager.UpdateAsync(id, dto);
        return result == null ? BadRequest(_manager.ErrorMsg) : Ok(result);
    }

    /// <summary>
    /// Delete resource
    /// </summary>
    /// <param name="id">Resource id</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteResource(Guid id)
    {
        var success = await _manager.DeleteAsync(id);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }
}
