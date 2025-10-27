using CommonMod.Models.AuditLogDtos;
using EntityFramework.DBProvider;

namespace CommonMod.Managers;

/// <summary>
/// Manager for audit log operations
/// </summary>
public class AuditLogManager(
    DefaultDbContext dbContext,
    ILogger<AuditLogManager> logger
) : ManagerBase<DefaultDbContext, AuditLog>(dbContext, logger)
{
    /// <summary>
    /// Get paged audit logs
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of audit logs</returns>
    public async Task<PageList<AuditLogItemDto>> GetPageAsync(AuditLogFilterDto filter)
    {
        Queryable = Queryable
            .WhereIf(filter.Category != null, q => q.Category == filter.Category)
            .WhereIf(filter.Event != null, q => q.Event == filter.Event)
            .WhereIf(filter.SubjectId != null, q => q.SubjectId == filter.SubjectId)
            .WhereIf(filter.StartDate != null, q => q.CreatedTime >= filter.StartDate)
            .WhereIf(filter.EndDate != null, q => q.CreatedTime <= filter.EndDate);

        return await ToPageAsync<AuditLogFilterDto, AuditLogItemDto>(filter);
    }

    /// <summary>
    /// Get audit log detail by id
    /// </summary>
    /// <param name="id">Audit log id</param>
    /// <returns>Audit log detail or null</returns>
    public async Task<AuditLogDetailDto?> GetDetailAsync(Guid id)
    {
        return await FindAsync<AuditLogDetailDto>(q => q.Id == id);
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
            UserAgent = userAgent
        };

        return await AddAsync(auditLog);
    }
}
