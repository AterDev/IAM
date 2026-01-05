namespace AccessMod.Models.AuthorizationDtos;

/// <summary>
/// User authorization DTO
/// </summary>
public class UserAuthorizationDto
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
}
