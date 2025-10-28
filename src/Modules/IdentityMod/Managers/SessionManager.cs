using CommonMod.Managers;
using Entity.IdentityMod;
using EntityFramework.DBProvider;
using IdentityMod.Models.LoginSessionDtos;
using System.Text.Json;

namespace IdentityMod.Managers;

/// <summary>
/// Manager for login session operations
/// </summary>
public class SessionManager(
    DefaultDbContext dbContext,
    ILogger<SessionManager> logger,
    AuditLogManager auditLogManager)
    : ManagerBase<DefaultDbContext, LoginSession>(dbContext, logger)
{
    private readonly AuditLogManager _auditLogManager = auditLogManager;

    /// <summary>
    /// Get paged login sessions
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of login sessions</returns>
    public async Task<PageList<LoginSessionItemDto>> GetPageAsync(LoginSessionFilterDto filter)
    {
        Queryable = Queryable
            .WhereNotNull(filter.UserId != null, q => q.UserId == filter.UserId)
            .WhereNotNull(filter.SessionId != null, q => q.SessionId == filter.SessionId)
            .WhereNotNull(filter.IpAddress != null, q => q.IpAddress != null && q.IpAddress.Contains(filter.IpAddress!))
            .WhereNotNull(filter.IsActive != null, q => q.IsActive == filter.IsActive)
            .WhereNotNull(filter.StartDate != null, q => q.LoginTime >= filter.StartDate)
            .WhereNotNull(filter.EndDate != null, q => q.LoginTime <= filter.EndDate);

        return await ToPageAsync<LoginSessionFilterDto, LoginSessionItemDto>(filter);
    }

    /// <summary>
    /// Get login session detail by id
    /// </summary>
    /// <param name="id">Login session id</param>
    /// <returns>Login session detail or null</returns>
    public async Task<LoginSessionDetailDto?> GetDetailAsync(Guid id)
    {
        return await FindAsync<LoginSessionDetailDto>(q => q.Id == id);
    }

    /// <summary>
    /// Get login session by session ID
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Login session detail or null</returns>
    public async Task<LoginSessionDetailDto?> GetBySessionIdAsync(string sessionId)
    {
        return await FindAsync<LoginSessionDetailDto>(q => q.SessionId == sessionId);
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
        string? userAgent = null)
    {
        var loginTime = DateTimeOffset.UtcNow;
        var loginSession = new LoginSession
        {
            UserId = dto.UserId,
            SessionId = dto.SessionId,
            IpAddress = dto.IpAddress,
            UserAgent = dto.UserAgent,
            DeviceInfo = dto.DeviceInfo,
            LoginTime = loginTime,
            LastActivityTime = loginTime,
            ExpirationTime = dto.ExpirationTime,
            IsActive = true
        };

        var result = await AddAsync(loginSession);
        if (!result)
        {
            return null;
        }

        // Write audit log for session creation
        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "SessionCreated",
            subjectId: dto.UserId.ToString(),
            payload: JsonSerializer.Serialize(new { sessionId = dto.SessionId, ipAddress = dto.IpAddress }),
            ipAddress: ipAddress ?? dto.IpAddress,
            userAgent: userAgent ?? dto.UserAgent
        );

        return await GetDetailAsync(loginSession.Id);
    }

    /// <summary>
    /// Update last activity time for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>True if successful</returns>
    public async Task<bool> UpdateLastActivityAsync(string sessionId)
    {
        var session = await FindAsync(q => q.SessionId == sessionId);
        if (session == null || !session.IsActive)
        {
            return false;
        }

        session.LastActivityTime = DateTimeOffset.UtcNow;
        return await UpdateAsync(session);
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
        string? userAgent = null)
    {
        var session = await FindAsync(q => q.Id == id);
        if (session == null)
        {
            return false;
        }

        session.IsActive = false;
        var result = await UpdateAsync(session);

        if (result)
        {
            // Write audit log for session revocation
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "SessionRevoked",
                subjectId: session.UserId.ToString(),
                payload: JsonSerializer.Serialize(new { sessionId = session.SessionId, revokedBy = revokedBy ?? "system" }),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
        }

        return result;
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
        string? userAgent = null)
    {
        var query = _dbSet.Where(q => q.UserId == userId && q.IsActive);
        
        if (!string.IsNullOrEmpty(exceptSessionId))
        {
            query = query.Where(q => q.SessionId != exceptSessionId);
        }

        var sessions = await query.ToListAsync();
        var count = 0;

        foreach (var session in sessions)
        {
            session.IsActive = false;
            if (await UpdateAsync(session))
            {
                count++;
            }
        }

        if (count > 0)
        {
            // Write audit log for bulk session revocation
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "BulkSessionRevoked",
                subjectId: userId.ToString(),
                payload: JsonSerializer.Serialize(new { count, revokedBy = revokedBy ?? "system" }),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
        }

        return count;
    }

    /// <summary>
    /// Clean up expired sessions
    /// </summary>
    /// <returns>Number of sessions cleaned up</returns>
    public async Task<int> CleanupExpiredSessionsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredSessions = await _dbSet
            .Where(q => q.IsActive && q.ExpirationTime != null && q.ExpirationTime < now)
            .ToListAsync();

        var count = 0;
        foreach (var session in expiredSessions)
        {
            session.IsActive = false;
            if (await UpdateAsync(session))
            {
                count++;
            }
        }

        if (count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", count);
        }

        return count;
    }
}
