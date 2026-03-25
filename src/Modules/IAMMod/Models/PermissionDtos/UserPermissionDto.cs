using Entity.IAMMod;

namespace IAMMod.Models.PermissionDtos;

/// <summary>
/// Current user's effective permission item.
/// </summary>
public class UserPermissionDto
{
    public required string Code { get; set; }
    public PermissionType Type { get; set; }
    public string? OwnedClientCode { get; set; }
}