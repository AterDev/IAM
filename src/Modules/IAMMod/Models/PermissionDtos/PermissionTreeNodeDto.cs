using Entity.IAMMod;

namespace IAMMod.Models.PermissionDtos;

/// <summary>
/// Permission tree node.
/// </summary>
public class PermissionTreeNodeDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public PermissionType Type { get; set; }
    public Guid? ParentId { get; set; }
    public string? Path { get; set; }
    public Guid? OwnedClientId { get; set; }
    public string? OwnedClientCode { get; set; }
    public bool Selected { get; set; }
    public List<PermissionTreeNodeDto> Children { get; set; } = [];
}