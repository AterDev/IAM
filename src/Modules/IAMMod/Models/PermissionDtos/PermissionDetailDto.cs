using Entity.IAMMod;

namespace IAMMod.Models.PermissionDtos;

/// <summary>
/// Permission detail.
/// </summary>
public class PermissionDetailDto : PermissionItemDto
{

    public new static PermissionDetailDto FromEntity(Permission permission) => new()
    {
        Id = permission.Id,
        Code = permission.Code,
        Name = permission.Name,
        Description = permission.Description,
        Type = permission.Type,
        ParentId = permission.ParentId,
        ParentCode = permission.Parent != null ? permission.Parent.Code : null,
        Path = permission.Path,
        OwnedClientId = permission.OwnedClientId,
        OwnedClientCode = permission.OwnedClient != null ? permission.OwnedClient.ClientId : null,
        CreatedTime = permission.CreatedTime,
        UpdatedTime = permission.UpdatedTime,
    };
}