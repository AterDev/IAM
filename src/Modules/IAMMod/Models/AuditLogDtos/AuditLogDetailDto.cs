namespace IAMMod.Models.AuditLogDtos;

/// <summary>
/// Audit log detail DTO
/// </summary>
public class AuditLogDetailDto
{
    public Guid Id { get; set; }
    public required string Category { get; set; }
    public required string Event { get; set; }
    public string? SubjectId { get; set; }
    public string? Payload { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedTime { get; set; }

    public static AuditLogDetailDto FromEntity(AuditLog entity) => new()
    {
        Id = entity.Id,
        Category = entity.Category,
        Event = entity.Event,
        SubjectId = entity.SubjectId,
        Payload = entity.Payload,
        IpAddress = entity.IpAddress,
        UserAgent = entity.UserAgent,
        CreatedTime = entity.CreatedTime,
    };
}
