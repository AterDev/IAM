using Entity.IAMMod;

namespace IAMMod.Models.PermissionDtos;

/// <summary>
/// Permission create/update payload.
/// </summary>
public class PermissionUpsertDto
{
    [StringLength(200)]
    public required string Code { get; set; }

    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public PermissionType Type { get; set; }
    public Guid? ParentId { get; set; }

    [StringLength(500)]
    public string? Path { get; set; }
    public Guid? OwnedClientId { get; set; }
}