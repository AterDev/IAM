namespace IdentityMod.Models.UserDtos;

/// <summary>
/// User filter DTO
/// </summary>
public class UserFilterDto : FilterBase
{
    /// <summary>
    /// Filter by username
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Filter by email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Filter by phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Filter by lockout status
    /// </summary>
    public bool? LockoutEnabled { get; set; }

    /// <summary>
    /// Filter by date range start
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// Filter by date range end
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }
}
