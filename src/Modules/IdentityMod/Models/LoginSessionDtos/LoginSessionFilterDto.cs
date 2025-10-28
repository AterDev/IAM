namespace IdentityMod.Models.LoginSessionDtos;

/// <summary>
/// Login session filter DTO
/// </summary>
public class LoginSessionFilterDto : FilterBase
{
    /// <summary>
    /// Filter by user ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Filter by session ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Filter by IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filter by date range start
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// Filter by date range end
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }
}
