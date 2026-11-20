using Perigon.PostgreSQL;
using IAMMod.Models.AuditLogDtos;

namespace IAMMod.Managers;

/// <summary>
/// Manager for audit log operations
/// </summary>
public class AuditLogManager(
    AppDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<AuditLogManager> logger
) : ManagerBase<DefaultDbContext, AuditLog>(dbContextFactory, userContext, logger)
{
    /// <summary>
    /// Get paged audit logs
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of audit logs</returns>
    public async Task<PageList<AuditLogItemDto>> GetPageAsync(AuditLogFilterDto filter)
    {
        var query = _dbSet
            .WhereNotNull(filter.Category, q => q.Category == filter.Category)
            .WhereNotNull(filter.Event, q => q.Event == filter.Event)
            .WhereNotNull(filter.SubjectId, q => q.SubjectId == filter.SubjectId)
            .WhereNotNull(filter.StartDate, q => q.CreatedTime >= filter.StartDate)
            .WhereNotNull(filter.EndDate, q => q.CreatedTime <= filter.EndDate);

        var count = await query.CountAsync();
        var rows = await query.OrderByDescending(item => item.CreatedTime).Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize).ToListAsync();
        return new PageList<AuditLogItemDto> { Count = count, PageIndex = filter.PageIndex, Data = rows.Select(AuditLogItemDto.FromEntity).ToList() };
    }

    /// <summary>
    /// Check if user has permission to access audit log
    /// </summary>
    /// <param name="id">Audit log id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        return await Task.FromResult(_userContext.IsAdmin);
    }

    /// <summary>
    /// Get audit log detail by id
    /// </summary>
    /// <param name="id">Audit log id</param>
    /// <returns>Audit log detail or null</returns>
    public async Task<AuditLogDetailDto?> GetDetailAsync(Guid id)
    {
        var entity = await FindAsync(id);
        return entity is null ? null : AuditLogDetailDto.FromEntity(entity);
    }

    /// <summary>
    /// Add new audit log entry
    /// </summary>
    /// <param name="category">Event category</param>
    /// <param name="eventName">Event name</param>
    /// <param name="subjectId">Subject identifier</param>
    /// <param name="payload">Additional data</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="userAgent">User agent</param>
    /// <returns>True if successful</returns>
    public async Task<bool> AddAuditLogAsync(
        string category,
        string eventName,
        string? subjectId = null,
        string? payload = null,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var auditLog = new AuditLog
        {
            Category = category,
            Event = eventName,
            SubjectId = subjectId,
            Payload = payload,
            IpAddress = ipAddress,
            UserAgent = userAgent,
        };

        await InsertAsync(auditLog);
        return true;
    }
}
