using Entity.IAMMod;

namespace IAMMod.Models.PermissionDtos;

/// <summary>
/// Client menu/button permission sync node.
/// </summary>
public class PermissionSyncNodeDto
{
    [MaxLength(200)]
    public required string Code { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public PermissionType Type { get; set; } = PermissionType.Menu;

    [MaxLength(100)]
    public string? Namespace { get; set; }

    [MaxLength(100)]
    public string? Resource { get; set; }

    [MaxLength(100)]
    public string? Action { get; set; }

    [MaxLength(500)]
    public string? Path { get; set; }

    [MaxLength(100)]
    public string? Icon { get; set; }

    public int Sort { get; set; }
    public List<PermissionSyncNodeDto> Children { get; set; } = [];
}