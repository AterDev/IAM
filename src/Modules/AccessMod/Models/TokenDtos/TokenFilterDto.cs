namespace AccessMod.Models.TokenDtos;

/// <summary>
/// Token filter DTO
/// </summary>
public class TokenFilterDto : FilterBase
{
    /// <summary>
    /// Filter by token type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filter by subject ID
    /// </summary>
    public string? SubjectId { get; set; }

    /// <summary>
    /// Filter by authorization ID
    /// </summary>
    public Guid? AuthorizationId { get; set; }

    /// <summary>
    /// Filter by creation date start
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// Filter by creation date end
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }
}
