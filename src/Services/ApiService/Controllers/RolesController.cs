using IdentityMod.Managers;
using IdentityMod.Models.RoleDtos;
using Microsoft.AspNetCore.Authorization;

namespace ApiService.Controllers;

/// <summary>
/// Role management controller
/// </summary>
public class RolesController(
    Share.Localizer localizer,
    RoleManager manager,
    IUserContext user,
    ILogger<RolesController> logger
) : RestControllerBase<RoleManager>(localizer, manager, user, logger)
{
    /// <summary>
    /// Get paged roles
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of roles</returns>
    [HttpGet]
    public async Task<ActionResult<PageList<RoleItemDto>>> GetRoles(
        [FromQuery] RoleFilterDto filter
    )
    {
        var result = await _manager.GetPageAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    /// <returns>List of all roles</returns>
    [HttpGet("all")]
    public async Task<ActionResult<List<RoleItemDto>>> GetAllRoles()
    {
        var result = await _manager.GetAllAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get role detail by id
    /// </summary>
    /// <param name="id">Role id</param>
    /// <returns>Role detail</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<RoleDetailDto>> GetDetail(Guid id)
    {
        var result = await _manager.GetDetailAsync(id);
        return result == null ? NotFound("Role not found") : Ok(result);
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    /// <param name="name">Role name</param>
    /// <returns>Role detail</returns>
    [HttpGet("name/{name}")]
    public async Task<ActionResult<RoleDetailDto>> GetByName(string name)
    {
        var result = await _manager.GetByNameAsync(name);
        return result == null ? NotFound("Role not found") : Ok(result);
    }

    /// <summary>
    /// Create new role
    /// </summary>
    /// <param name="dto">Role data</param>
    /// <returns>Created role detail</returns>
    [HttpPost]
    public async Task<ActionResult<RoleDetailDto>> CreateRole([FromBody] RoleAddDto dto)
    {
        var result = await _manager.AddAsync(dto);
        return result == null
            ? BadRequest(_manager.ErrorMsg)
            : CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update role
    /// </summary>
    /// <param name="id">Role id</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated role detail</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<RoleDetailDto>> UpdateRole(
        Guid id,
        [FromBody] RoleUpdateDto dto
    )
    {
        var result = await _manager.UpdateAsync(id, dto);
        return result == null ? BadRequest(_manager.ErrorMsg) : Ok(result);
    }

    /// <summary>
    /// Delete role
    /// </summary>
    /// <param name="id">Role id</param>
    /// <param name="hardDelete">Perform hard delete (default false)</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRole(Guid id, [FromQuery] bool hardDelete = false)
    {
        var success = await _manager.DeleteAsync(id, !hardDelete);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }

    /// <summary>
    /// Grant permissions to role
    /// </summary>
    /// <param name="id">Role id</param>
    /// <param name="dto">Grant permission DTO</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id}/permissions")]
    public async Task<ActionResult> GrantPermissions(
        Guid id,
        [FromBody] RoleGrantPermissionDto dto
    )
    {
        var success = await _manager.GrantPermissionsAsync(id, dto);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }

    /// <summary>
    /// Get role permissions
    /// </summary>
    /// <param name="id">Role id</param>
    /// <returns>List of permissions</returns>
    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<List<PermissionClaim>>> GetPermissions(Guid id)
    {
        var result = await _manager.GetPermissionsAsync(id);
        return Ok(result);
    }
}
