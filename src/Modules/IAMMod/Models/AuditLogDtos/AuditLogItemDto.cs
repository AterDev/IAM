namespace IAMMod.Models.AuditLogDtos;

/// <summary>
/// Audit log item DTO for list views
/// </summary>
public class AuditLogItemDto
{
    public Guid Id { get; set; }
    public required string Category { get; set; }
    public required string Event { get; set; }
    public string? SubjectId { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedTime { get; set; }

    public static AuditLogItemDto FromEntity(AuditLog entity) => new()
    {
        Id = entity.Id,
        Category = entity.Category,
        Event = entity.Event,
        SubjectId = entity.SubjectId,
        IpAddress = entity.IpAddress,
        CreatedTime = entity.CreatedTime,
    };
}
