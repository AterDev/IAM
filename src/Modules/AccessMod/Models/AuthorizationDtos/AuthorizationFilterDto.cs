namespace AccessMod.Models.AuthorizationDtos;

/// <summary>
/// Authorization filter DTO
/// </summary>
public class AuthorizationFilterDto : FilterBase
{
    /// <summary>
    /// Filter by subject ID
    /// </summary>
    public string? SubjectId { get; set; }

    /// <summary>
    /// Filter by client ID
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filter by creation date start
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// Filter by creation date end
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }
}
