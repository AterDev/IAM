using UserCenterMod.Managers;
using UserCenterMod.Models;

namespace ApiService.Controllers.UserCenterMod;

/// <summary>
/// Administrator API for entitlement definitions.
/// </summary>
public class UserEntitlementDefinitionsController(
    Localizer localizer,
    UserEntitlementDefinitionManager manager,
    IUserContext user,
    ILogger<UserEntitlementDefinitionsController> logger
) : RestControllerBase<UserEntitlementDefinitionManager>(localizer, manager, user, logger)
{
    [HttpGet]
    public Task<PageList<UserEntitlementDefinitionItemDto>> GetPage(
        [FromQuery] UserEntitlementDefinitionFilterDto filter,
        CancellationToken cancellationToken) => _manager.GetPageAsync(filter, cancellationToken);

    [HttpGet("{id}")]
    public async Task<ActionResult<UserEntitlementDefinitionItemDto>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var item = await _manager.GetDetailAsync(id, cancellationToken);
        return item is null ? NotFound("NotFound") : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<UserEntitlementDefinitionItemDto>> Create(
        [FromBody] UserEntitlementDefinitionUpsertDto dto,
        CancellationToken cancellationToken)
    {
        var item = await _manager.AddAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetDetail), new { id = item.Id }, item);
    }

    [HttpPut("{id}")]
    public Task<UserEntitlementDefinitionItemDto> Update(
        Guid id,
        [FromBody] UserEntitlementDefinitionUpsertDto dto,
        CancellationToken cancellationToken) => _manager.UpdateAsync(id, dto, cancellationToken);

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _manager.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
