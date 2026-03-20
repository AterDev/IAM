using IAMMod.Managers;
using IAMMod.Models.PermissionDtos;

namespace ApiService.Controllers.IAMMod;

/// <summary>
/// Unified permission management controller.
/// </summary>
public class PermissionsController(
    Localizer localizer,
    PermissionManager manager,
    IUserContext user,
    ILogger<PermissionsController> logger)
    : RestControllerBase<PermissionManager>(localizer, manager, user, logger)
{
    /// <summary>
    /// Get paged permissions.
    /// </summary>
    [HttpGet]
    public Task<PageList<PermissionItemDto>> GetPermissions([FromQuery] PermissionFilterDto filter)
    {
        return _manager.GetPageAsync(filter);
    }

    /// <summary>
    /// Get permission tree.
    /// </summary>
    [HttpGet("tree")]
    public Task<List<PermissionTreeNodeDto>> GetPermissionTree([FromQuery] PermissionFilterDto filter)
    {
        return _manager.GetTreeAsync(filter);
    }

    /// <summary>
    /// Get current user's menu tree for a client.
    /// </summary>
    [HttpGet("my-menu-tree")]
    public async Task<ActionResult<List<PermissionTreeNodeDto>>> GetMyMenuTree([FromQuery] string clientCode)
    {
        if (_user.UserId == Guid.Empty)
        {
            return Unauthorized();
        }

        return await _manager.GetCurrentUserMenuTreeAsync(_user.UserId, clientCode);
    }

    /// <summary>
    /// Get permission detail.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PermissionDetailDto>> GetDetail(Guid id)
    {
        var result = await _manager.GetDetailAsync(id);
        return result == null ? NotFound("Permission not found") : Ok(result);
    }

    /// <summary>
    /// Create permission.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PermissionDetailDto>> Create([FromBody] PermissionUpsertDto dto)
    {
        var result = await _manager.AddAsync(dto);
        return result == null
            ? Problem("Failed to create permission", statusCode: StatusCodes.Status400BadRequest)
            : CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update permission.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PermissionDetailDto>> Update(Guid id, [FromBody] PermissionUpsertDto dto)
    {
        var result = await _manager.UpdateAsync(id, dto);
        return result == null
            ? Problem("Failed to update permission", statusCode: StatusCodes.Status400BadRequest)
            : Ok(result);
    }

    /// <summary>
    /// Delete permission.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _manager.DeleteAsync(id);
        return result ? NoContent() : Problem("Failed to delete permission", statusCode: StatusCodes.Status400BadRequest);
    }
}