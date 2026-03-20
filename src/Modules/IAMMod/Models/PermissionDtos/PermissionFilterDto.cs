using Entity.IAMMod;

namespace IAMMod.Models.PermissionDtos;

/// <summary>
/// Permission filter.
/// </summary>
public class PermissionFilterDto : FilterBase
{
    /// <summary>
    /// Filter by database client id.
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    /// Filter by public client identifier.
    /// </summary>
    public string? ClientCode { get; set; }

    /// <summary>
    /// Filter by permission type.
    /// </summary>
    public PermissionType? Type { get; set; }

    /// <summary>
    /// Filter by parent permission id.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Filter by keyword.
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Whether to include only menu and button permissions.
    /// </summary>
    public bool? OnlyNonBusiness { get; set; }
}