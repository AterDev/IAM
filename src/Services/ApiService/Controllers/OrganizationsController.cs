using IdentityMod.Managers;
using IdentityMod.Models.OrganizationDtos;
using Microsoft.AspNetCore.Authorization;

namespace ApiService.Controllers;

/// <summary>
/// Organization management controller
/// </summary>
public class OrganizationsController(
    Share.Localizer localizer,
    OrganizationManager manager,
    IUserContext user,
    ILogger<OrganizationsController> logger
) : RestControllerBase<OrganizationManager>(localizer, manager, user, logger)
{
    /// <summary>
    /// Get paged organizations
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of organizations</returns>
    [HttpGet]
    public async Task<ActionResult<PageList<OrganizationItemDto>>> GetOrganizations(
        [FromQuery] OrganizationFilterDto filter
    )
    {
        var result = await _manager.GetPageAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get organization tree
    /// </summary>
    /// <param name="parentId">Parent organization id (null for root)</param>
    /// <returns>Organization tree</returns>
    [HttpGet("tree")]
    public async Task<ActionResult<List<OrganizationTreeDto>>> GetTree(
        [FromQuery] Guid? parentId = null
    )
    {
        var result = await _manager.GetTreeAsync(parentId);
        return Ok(result);
    }

    /// <summary>
    /// Get organization detail by id
    /// </summary>
    /// <param name="id">Organization id</param>
    /// <returns>Organization detail</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationDetailDto>> GetDetail(Guid id)
    {
        var result = await _manager.GetDetailAsync(id);
        return result == null ? NotFound("Organization not found") : Ok(result);
    }

    /// <summary>
    /// Create new organization
    /// </summary>
    /// <param name="dto">Organization data</param>
    /// <returns>Created organization detail</returns>
    [HttpPost]
    public async Task<ActionResult<OrganizationDetailDto>> CreateOrganization(
        [FromBody] OrganizationAddDto dto
    )
    {
        var result = await _manager.AddAsync(dto);
        return result == null
            ? BadRequest(_manager.ErrorMsg)
            : CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update organization
    /// </summary>
    /// <param name="id">Organization id</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated organization detail</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<OrganizationDetailDto>> UpdateOrganization(
        Guid id,
        [FromBody] OrganizationUpdateDto dto
    )
    {
        var result = await _manager.UpdateAsync(id, dto);
        return result == null ? BadRequest(_manager.ErrorMsg) : Ok(result);
    }

    /// <summary>
    /// Delete organization
    /// </summary>
    /// <param name="id">Organization id</param>
    /// <param name="hardDelete">Perform hard delete (default false)</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOrganization(
        Guid id,
        [FromQuery] bool hardDelete = false
    )
    {
        var success = await _manager.DeleteAsync(id, !hardDelete);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }

    /// <summary>
    /// Add users to organization
    /// </summary>
    /// <param name="id">Organization id</param>
    /// <param name="userIds">User ids to add</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id}/users")]
    public async Task<ActionResult> AddUsers(Guid id, [FromBody] List<Guid> userIds)
    {
        var success = await _manager.AddUsersAsync(id, userIds);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }

    /// <summary>
    /// Remove users from organization
    /// </summary>
    /// <param name="id">Organization id</param>
    /// <param name="userIds">User ids to remove</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}/users")]
    public async Task<ActionResult> RemoveUsers(Guid id, [FromBody] List<Guid> userIds)
    {
        var success = await _manager.RemoveUsersAsync(id, userIds);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }
}
