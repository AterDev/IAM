namespace Entity;

/// <summary>
/// Audit log entity for tracking system events
/// </summary>
[Modules(Modules.Common)]
public class AuditLog : EntityBase
{
    /// <summary>
    /// Event category
    /// </summary>
    public required string Category { get; set; }
    
    /// <summary>
    /// Event name or action
    /// </summary>
    public required string Event { get; set; }
    
    /// <summary>
    /// Subject identifier (user id or system)
    /// </summary>
    public string? SubjectId { get; set; }
    
    /// <summary>
    /// Additional payload data (JSON format)
    /// </summary>
    public string? Payload { get; set; }
    
    /// <summary>
    /// IP address of the request
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }
}
