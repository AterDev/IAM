namespace AccessMod.Models.TokenDtos;

/// <summary>
/// Token item DTO for list display
/// </summary>
public class TokenItemDto
{
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public string? Status { get; set; }
    public string? SubjectId { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
}
