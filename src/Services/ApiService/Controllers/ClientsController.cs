using AccessMod.Managers;
using AccessMod.Models.AuthorizationDtos;
using AccessMod.Models.ClientDtos;

namespace ApiService.Controllers;

/// <summary>
/// OAuth/OIDC client management controller
/// </summary>
[Route("api/[controller]")]
public class ClientsController(
    Share.Localizer localizer,
    ClientManager manager,
    IUserContext user,
    ILogger<ClientsController> logger
) : RestControllerBase<ClientManager>(localizer, manager, user, logger)
{
    /// <summary>
    /// Get paged clients
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of clients</returns>
    [HttpGet]
    public async Task<ActionResult<PageList<ClientItemDto>>> GetClients(
        [FromQuery] ClientFilterDto filter
    )
    {
        var result = await _manager.GetPageAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get client detail by id
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>Client detail</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ClientDetailDto>> GetDetail(Guid id)
    {
        var result = await _manager.GetDetailAsync(id);
        return result == null ? NotFound("Client not found") : Ok(result);
    }

    /// <summary>
    /// Create new client
    /// </summary>
    /// <param name="dto">Client data</param>
    /// <returns>Created client detail with secret</returns>
    [HttpPost]
    public async Task<ActionResult<object>> CreateClient([FromBody] ClientAddDto dto)
    {
        var (detail, secret) = await _manager.AddAsync(dto);
        if (detail == null || secret == null)
        {
            return BadRequest(_manager.ErrorMsg);
        }

        return CreatedAtAction(
            nameof(GetDetail),
            new { id = detail.Id },
            new { client = detail, secret }
        );
    }

    /// <summary>
    /// Update client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated client detail</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ClientDetailDto>> UpdateClient(
        Guid id,
        [FromBody] ClientUpdateDto dto
    )
    {
        var result = await _manager.UpdateAsync(id, dto);
        return result == null ? BadRequest(_manager.ErrorMsg) : Ok(result);
    }

    /// <summary>
    /// Delete client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteClient(Guid id)
    {
        var success = await _manager.DeleteAsync(id);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }

    /// <summary>
    /// Rotate client secret
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>New client secret</returns>
    [HttpPost("{id}/secret:rotate")]
    public async Task<ActionResult<ClientSecretDto>> RotateSecret(Guid id)
    {
        var newSecret = await _manager.RotateSecretAsync(id);
        if (newSecret == null)
        {
            return BadRequest(_manager.ErrorMsg);
        }

        return Ok(new ClientSecretDto { Secret = newSecret });
    }

    /// <summary>
    /// Assign scopes to client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <param name="dto">Scope assignment data</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id}/scopes")]
    public async Task<ActionResult> AssignScopes(Guid id, [FromBody] ClientScopeAssignDto dto)
    {
        var success = await _manager.AssignScopesAsync(id, dto.ScopeIds);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }

    /// <summary>
    /// Get client authorizations
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>List of authorizations</returns>
    [HttpGet("{id}/authorizations")]
    public async Task<ActionResult<List<AuthorizationItemDto>>> GetAuthorizations(Guid id)
    {
        var result = await _manager.GetAuthorizationsAsync(id);
        return Ok(result);
    }
}
