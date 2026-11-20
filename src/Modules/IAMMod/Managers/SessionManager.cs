using System.Text.Json.Nodes;
using IAMMod.Models.LoginSessionDtos;
using Microsoft.AspNetCore.Http;
using Share.Exceptions;
using System.Text.Json;

namespace IAMMod.Managers;

/// <summary>
/// Manager for login session operations
/// </summary>
public class SessionManager(
    AppDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<SessionManager> logger,
    AuditLogManager auditLogManager,
    SessionValidationService sessionValidationService
) : ManagerBase<DefaultDbContext, LoginSession>(dbContextFactory, userContext, logger)
{
    private readonly AuditLogManager _auditLogManager = auditLogManager;
    private readonly SessionValidationService _sessionValidationService = sessionValidationService;

    /// <summary>
    /// Get paged login sessions
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of login sessions</returns>
    public async Task<PageList<LoginSessionItemDto>> GetPageAsync(LoginSessionFilterDto filter)
    {
        var query = _dbSet
            .WhereNotNull(filter.UserId, q => q.UserId == filter.UserId)
            .WhereNotNull(filter.SessionId, q => q.SessionId == filter.SessionId)
            .WhereNotNull(
                filter.IpAddress,
                q => q.IpAddress != null && q.IpAddress.Contains(filter.IpAddress!)
            )
            .WhereNotNull(filter.IsActive, q => q.IsActive == filter.IsActive)
            .WhereNotNull(filter.StartDate, q => q.LoginTime >= filter.StartDate)
            .WhereNotNull(filter.EndDate, q => q.LoginTime <= filter.EndDate);

        var count = await query.CountAsync();
        var sessions = await query.OrderByDescending(item => item.CreatedTime)
            .Skip((filter.PageIndex - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();
        return new PageList<LoginSessionItemDto> { Count = count, PageIndex = filter.PageIndex, Data = sessions.Select(LoginSessionItemDto.FromEntity).ToList() };
    }

    /// <summary>
    /// Check if user has permission to access login session
    /// </summary>
    /// <param name="id">Login session id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        return await Task.FromResult(_userContext.IsAdmin);
    }

    /// <summary>
    /// Get login session detail by id
    /// </summary>
    /// <param name="id">Login session id</param>
    /// <returns>Login session detail or null</returns>
    public async Task<LoginSessionDetailDto?> GetDetailAsync(Guid id)
    {
        var session = await _dbSet.Where(q => q.Id == id).FirstOrDefaultAsync();
        return session is null ? null : LoginSessionDetailDto.FromEntity(session);
    }

    /// <summary>
    /// Get login session by session ID
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Login session detail or null</returns>
    public async Task<LoginSessionDetailDto?> GetBySessionIdAsync(string sessionId)
    {
        var session = await _dbSet.Where(q => q.SessionId == sessionId).FirstOrDefaultAsync();
        return session is null ? null : LoginSessionDetailDto.FromEntity(session);
    }

    /// <summary>
    /// Add new login session
    /// </summary>
    /// <param name="dto">Login session add DTO</param>
    /// <param name="ipAddress">IP address for audit log</param>
    /// <param name="userAgent">User agent for audit log</param>
    /// <returns>Created login session detail or null</returns>
    public async Task<LoginSessionDetailDto?> AddAsync(
        LoginSessionAddDto dto,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var loginTime = DateTimeOffset.UtcNow;
        var loginSession = new LoginSession
        {
            UserId = dto.UserId,
            SessionId = dto.SessionId,
            IpAddress = dto.IpAddress,
            UserAgent = dto.UserAgent,
            DeviceInfo = dto.DeviceInfo,
            ExpirationTime = dto.ExpirationTime,
        };
        loginSession.LoginTime = loginTime;
        loginSession.LastActivityTime = loginTime;
        loginSession.IsActive = true;

        await InsertAsync(loginSession);
        await _sessionValidationService.SetStateAsync(loginSession);

        // Write audit log for session creation
        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "SessionCreated",
            subjectId: dto.UserId.ToString(),
            payload: new JsonObject { ["sessionId"] = dto.SessionId, ["ipAddress"] = dto.IpAddress }.ToJsonString(),
            ipAddress: ipAddress ?? dto.IpAddress,
            userAgent: userAgent ?? dto.UserAgent
        );

        return await GetDetailAsync(loginSession.Id);
    }

    /// <summary>
    /// Revoke a login session
    /// </summary>
    /// <param name="id">Login session id</param>
    /// <param name="revokedBy">User ID who revoked the session</param>
    /// <param name="ipAddress">IP address for audit log</param>
    /// <param name="userAgent">User agent for audit log</param>
    /// <returns>True if successful</returns>
    public async Task<bool> RevokeSessionAsync(
        Guid id,
        string? revokedBy = null,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var session = await FindAsync(id);
        if (session == null)
        {
            throw new BusinessException("SessionNotFound", StatusCodes.Status404NotFound);
        }

        session.IsActive = false;
        session.UpdatedTime = DateTime.UtcNow;
        await _dbSet.Where(item => item.Id == id).ExecuteUpdateAsync(update => update
            .Set(item => item.IsActive, false).Set(item => item.UpdatedTime, session.UpdatedTime));
        await _sessionValidationService.SetStateAsync(session);

        // Write audit log for session revocation
        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "SessionRevoked",
            subjectId: session.UserId.ToString(),
            payload: new JsonObject { ["sessionId"] = session.SessionId, ["revokedBy"] = revokedBy ?? "system" }.ToJsonString(),
            ipAddress: ipAddress,
            userAgent: userAgent
        );

        return true;
    }

    /// <summary>
    /// Revoke all sessions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="exceptSessionId">Optional session ID to keep active</param>
    /// <param name="revokedBy">User ID who revoked the sessions</param>
    /// <param name="ipAddress">IP address for audit log</param>
    /// <param name="userAgent">User agent for audit log</param>
    /// <returns>Number of sessions revoked</returns>
    public async Task<int> RevokeAllUserSessionsAsync(
        Guid userId,
        string? exceptSessionId = null,
        string? revokedBy = null,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var query = _dbSet.Where(q => q.UserId == userId && q.IsActive);

        if (!string.IsNullOrEmpty(exceptSessionId))
        {
            query = query.Where(q => q.SessionId != exceptSessionId);
        }

        var sessions = await query.ToListAsync();
        var affectedSessions = sessions

            .Select(q => new SessionValidationCacheEntry
            {
                UserId = q.UserId,
                SessionId = q.SessionId,
                IsActive = false,
                LastActivityTime = q.LastActivityTime,
                LastPersistedActivityTime = q.LastActivityTime,
                ExpirationTime = q.ExpirationTime,
            }).ToList();

        var count = await query.ExecuteUpdateAsync(setters => setters
            .Set(s => s.IsActive, false)
            .Set(s => s.UpdatedTime, DateTimeOffset.UtcNow));

        foreach (var affectedSession in affectedSessions)
        {
            await _sessionValidationService.SetStateAsync(affectedSession);
        }

        if (count > 0)
        {
            // Write audit log for bulk session revocation
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "BulkSessionRevoked",
                subjectId: userId.ToString(),
                payload: new JsonObject { ["count"] = count, ["revokedBy"] = revokedBy ?? "system" }.ToJsonString(),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
        }

        return count;
    }

}
