namespace AccessMod.Models.TokenDtos;

/// <summary>
/// Token detail DTO
/// </summary>
public class TokenDetailDto
{
    public Guid Id { get; set; }
    public Guid? AuthorizationId { get; set; }
    public string? ReferenceId { get; set; }
    public required string Type { get; set; }
    public string? Status { get; set; }
    public string? SubjectId { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public DateTimeOffset? RedemptionDate { get; set; }
}
