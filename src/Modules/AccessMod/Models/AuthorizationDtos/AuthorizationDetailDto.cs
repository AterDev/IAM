namespace AccessMod.Models.AuthorizationDtos;

/// <summary>
/// Authorization detail DTO
/// </summary>
public class AuthorizationDetailDto
{
    public Guid Id { get; set; }
    public required string SubjectId { get; set; }
    public Guid ClientId { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public string? Scopes { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
}
