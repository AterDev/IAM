namespace IAMMod.Models.PermissionDtos;

/// <summary>
/// Full replacement payload for client menu/button permission sync.
/// </summary>
public class ClientPermissionSyncDto
{
    public List<PermissionSyncNodeDto> Permissions { get; set; } = [];
}