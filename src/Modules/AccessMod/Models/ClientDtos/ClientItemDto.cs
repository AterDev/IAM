namespace AccessMod.Models.ClientDtos;

/// <summary>
/// Client item DTO for list display
/// </summary>
public class ClientItemDto
{
    public Guid Id { get; set; }
    public required string ClientId { get; set; }
    public required string DisplayName { get; set; }
    public string? Type { get; set; }
    public string? ApplicationType { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}
