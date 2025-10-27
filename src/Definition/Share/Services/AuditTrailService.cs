using System.Text.Json;

namespace Share.Services;

/// <summary>
/// Implementation of audit trail service
/// </summary>
public class AuditTrailService : IAuditTrailService
{
    private readonly ILogger<AuditTrailService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AuditTrailService(
        ILogger<AuditTrailService> logger,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task LogEventAsync(
        string category,
        string eventName,
        string? subjectId = null,
        string? payload = null,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        try
        {
            // Use a scoped service to avoid circular dependencies
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EntityFramework.DBProvider.DefaultDbContext>();

            var auditLog = new Entity.AuditLog
            {
                Category = category,
                Event = eventName,
                SubjectId = subjectId,
                Payload = payload,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await dbContext.AuditLogs.AddAsync(auditLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event: {Category}/{Event}", category, eventName);
        }
    }

    public async Task LogAuthenticationAsync(
        string eventName,
        string userId,
        bool success,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var payload = JsonSerializer.Serialize(new
        {
            UserId = userId,
            Success = success
        });

        await LogEventAsync(
            "Authentication",
            eventName,
            userId,
            payload,
            ipAddress,
            userAgent
        );
    }

    public async Task LogAuthorizationAsync(
        string eventName,
        string userId,
        string resource,
        string action,
        bool success
    )
    {
        var payload = JsonSerializer.Serialize(new
        {
            UserId = userId,
            Resource = resource,
            Action = action,
            Success = success
        });

        await LogEventAsync(
            "Authorization",
            eventName,
            userId,
            payload
        );
    }
}
