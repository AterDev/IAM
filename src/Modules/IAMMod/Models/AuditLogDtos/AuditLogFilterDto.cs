namespace CommonMod.Models.AuditLogDtos;

/// <summary>
/// Audit log filter DTO
/// </summary>
public class AuditLogFilterDto : FilterBase
{
    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Filter by event
    /// </summary>
    public string? Event { get; set; }
    
    /// <summary>
    /// Filter by subject ID
    /// </summary>
    public string? SubjectId { get; set; }
    
    /// <summary>
    /// Filter by date range start
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }
    
    /// <summary>
    /// Filter by date range end
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }
}
