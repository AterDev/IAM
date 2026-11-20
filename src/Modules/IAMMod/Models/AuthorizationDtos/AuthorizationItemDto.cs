namespace IAMMod.Models.AuthorizationDtos;

/// <summary>
/// Authorization item DTO for list display
/// </summary>
public class AuthorizationItemDto
{
    public Guid Id { get; set; }
    public string SubjectId { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset CreationDate { get; set; }
}
