using CommonMod.Managers;
using CommonMod.Models.AuditLogDtos;
using Microsoft.AspNetCore.Mvc;

namespace ApiService.Controllers;

/// <summary>
/// Audit trail controller
/// </summary>
public class AuditTrailController(
    Share.Localizer localizer,
    AuditLogManager manager,
    IUserContext user,
    ILogger<AuditTrailController> logger
) : RestControllerBase<AuditLogManager>(localizer, manager, user, logger)
{
    /// <summary>
    /// Get paged audit logs
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of audit logs</returns>
    [HttpGet]
    public async Task<ActionResult<PageList<AuditLogItemDto>>> GetAuditLogs([FromQuery] AuditLogFilterDto filter)
    {
        var result = await _manager.GetPageAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get audit log detail by id
    /// </summary>
    /// <param name="id">Audit log id</param>
    /// <returns>Audit log detail</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<AuditLogDetailDto>> GetDetail(Guid id)
    {
        var result = await _manager.GetDetailAsync(id);
        if (result == null)
        {
            return NotFound("Audit log not found");
        }
        return Ok(result);
    }
}
