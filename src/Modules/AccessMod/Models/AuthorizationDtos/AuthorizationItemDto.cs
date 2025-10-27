namespace AccessMod.Models.AuthorizationDtos;

/// <summary>
/// Authorization item DTO for list display
/// </summary>
public class AuthorizationItemDto
{
    public Guid Id { get; set; }
    public required string SubjectId { get; set; }
    public Guid ClientId { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset CreationDate { get; set; }
}
