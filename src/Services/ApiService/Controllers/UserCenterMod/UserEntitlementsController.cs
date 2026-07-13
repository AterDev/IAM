using UserCenterMod.Managers;
using UserCenterMod.Models;

namespace ApiService.Controllers.UserCenterMod;

/// <summary>
/// Administrator API for a user's entitlement assignments.
/// </summary>
public class UserEntitlementsController(
    Localizer localizer,
    UserEntitlementManager manager,
    IUserContext user,
    ILogger<UserEntitlementsController> logger
) : RestControllerBase<UserEntitlementManager>(localizer, manager, user, logger)
{
    [HttpGet]
    public Task<PageList<UserEntitlementDetailDto>> GetPage(
        [FromQuery] UserEntitlementFilterDto filter,
        CancellationToken cancellationToken) => _manager.GetPageAsync(filter, cancellationToken);

    [HttpGet("{id}")]
    public async Task<ActionResult<UserEntitlementDetailDto>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var item = await _manager.GetDetailAsync(id, cancellationToken);
        return item is null ? NotFound("NotFound") : Ok(item);
    }

    [HttpPost("users/{userId}")]
    public async Task<ActionResult<UserEntitlementDetailDto>> Create(
        Guid userId,
        [FromBody] UserEntitlementAddDto dto,
        CancellationToken cancellationToken)
    {
        var item = await _manager.AddAsync(userId, dto, cancellationToken);
        return CreatedAtAction(nameof(GetDetail), new { id = item.Id }, item);
    }

    [HttpPut("{id}")]
    public Task<UserEntitlementDetailDto> Update(
        Guid id,
        [FromBody] UserEntitlementUpdateDto dto,
        CancellationToken cancellationToken) => _manager.UpdateAsync(id, dto, cancellationToken);

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _manager.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
