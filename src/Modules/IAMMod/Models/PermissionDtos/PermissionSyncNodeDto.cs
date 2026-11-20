using Entity.IAMMod;

namespace IAMMod.Models.PermissionDtos;

/// <summary>
/// Client menu/button permission sync node.
/// </summary>
public class PermissionSyncNodeDto
{
    [StringLength(200)]
    public required string Code { get; set; }

    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public PermissionType Type { get; set; } = PermissionType.Menu;

    [StringLength(500)]
    public string? Path { get; set; }
    public List<PermissionSyncNodeDto> Children { get; set; } = [];
}