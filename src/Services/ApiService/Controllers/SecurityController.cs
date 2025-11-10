using CommonMod.Managers;
using CommonMod.Models.AuditLogDtos;
using IdentityMod.Managers;
using IdentityMod.Models.LoginSessionDtos;

namespace ApiService.Controllers;

/// <summary>
/// Security controller for session and audit log management
/// </summary>
public class SecurityController(
    Share.Localizer localizer,
    SessionManager sessionManager,
    AuditLogManager auditLogManager,
    IUserContext user,
    ILogger<SecurityController> logger
) : RestControllerBase<SessionManager>(localizer, sessionManager, user, logger)
{
    private readonly AuditLogManager _auditLogManager = auditLogManager;

    /// <summary>
    /// Get paged login sessions
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of login sessions</returns>
    [HttpGet("sessions")]
    public async Task<ActionResult<PageList<LoginSessionItemDto>>> GetSessions(
        [FromQuery] LoginSessionFilterDto filter
    )
    {
        var result = await _manager.GetPageAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get login session detail by id
    /// </summary>
    /// <param name="id">Login session id</param>
    /// <returns>Login session detail</returns>
    [HttpGet("sessions/{id}")]
    public async Task<ActionResult<LoginSessionDetailDto>> GetSessionDetail(Guid id)
    {
        var result = await _manager.GetDetailAsync(id);
        return result == null ? NotFound("Login session not found") : Ok(result);
    }

    /// <summary>
    /// Revoke a login session
    /// </summary>
    /// <param name="id">Login session id</param>
    /// <returns>Success result</returns>
    [HttpPost("sessions/{id}/revoke")]
    public async Task<ActionResult> RevokeSession(Guid id)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var revokedBy = _user.UserId.ToString();

        var result = await _manager.RevokeSessionAsync(id, revokedBy, ipAddress, userAgent);

        return !result
            ? NotFound("Login session not found or already revoked")
            : Ok(new { message = "Session revoked successfully" });
    }

    /// <summary>
    /// Revoke all sessions for the current user
    /// </summary>
    /// <param name="exceptCurrent">Whether to keep the current session active</param>
    /// <returns>Number of sessions revoked</returns>
    [HttpPost("sessions/revoke-all")]
    public async Task<ActionResult> RevokeAllSessions([FromQuery] bool exceptCurrent = false)
    {
        if (_user.UserId == Guid.Empty)
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var currentSessionId = exceptCurrent ? HttpContext.User.FindFirst("sid")?.Value : null;

        var count = await _manager.RevokeAllUserSessionsAsync(
            _user.UserId,
            currentSessionId,
            _user.UserId.ToString(),
            ipAddress,
            userAgent
        );

        return Ok(new { message = $"Revoked {count} session(s)", count });
    }

    /// <summary>
    /// Get paged audit logs
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of audit logs</returns>
    [HttpPost("logs")]
    public async Task<ActionResult<PageList<AuditLogItemDto>>> GetAuditLogs(
        AuditLogFilterDto filter
    )
    {
        var result = await _auditLogManager.GetPageAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get audit log detail by id
    /// </summary>
    /// <param name="id">Audit log id</param>
    /// <returns>Audit log detail</returns>
    [HttpGet("logs/{id}")]
    public async Task<ActionResult<AuditLogDetailDto>> GetAuditLogDetail(Guid id)
    {
        var result = await _auditLogManager.GetDetailAsync(id);
        return result == null ? NotFound("Audit log not found") : Ok(result);
    }
}
