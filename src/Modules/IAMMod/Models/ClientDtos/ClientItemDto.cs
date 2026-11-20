using Entity.IAMMod;

namespace IAMMod.Models.ClientDtos;

/// <summary>
/// Client item DTO for list display
/// </summary>
public class ClientItemDto
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ClientType? Type { get; set; }
    public ApplicationType? ApplicationType { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}
