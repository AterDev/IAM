namespace Share.Services;

/// <summary>
/// Interface for audit trail service
/// </summary>
public interface IAuditTrailService
{
    /// <summary>
    /// Log an audit event
    /// </summary>
    /// <param name="category">Event category</param>
    /// <param name="eventName">Event name</param>
    /// <param name="subjectId">Subject identifier (user id)</param>
    /// <param name="payload">Additional payload data</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="userAgent">User agent</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task LogEventAsync(
        string category,
        string eventName,
        string? subjectId = null,
        string? payload = null,
        string? ipAddress = null,
        string? userAgent = null
    );

    /// <summary>
    /// Log authentication event
    /// </summary>
    /// <param name="eventName">Event name (e.g., "Login", "Logout")</param>
    /// <param name="userId">User identifier</param>
    /// <param name="success">Whether the event was successful</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="userAgent">User agent</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task LogAuthenticationAsync(
        string eventName,
        string userId,
        bool success,
        string? ipAddress = null,
        string? userAgent = null
    );

    /// <summary>
    /// Log authorization event
    /// </summary>
    /// <param name="eventName">Event name (e.g., "RoleAssigned", "PermissionGranted")</param>
    /// <param name="userId">User identifier</param>
    /// <param name="resource">Resource being accessed</param>
    /// <param name="action">Action being performed</param>
    /// <param name="success">Whether the event was successful</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task LogAuthorizationAsync(
        string eventName,
        string userId,
        string resource,
        string action,
        bool success
    );
}
