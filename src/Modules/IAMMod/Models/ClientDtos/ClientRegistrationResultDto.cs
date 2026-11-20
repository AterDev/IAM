namespace IAMMod.Models.ClientDtos;

/// <summary>
/// Result of a client self-service registration or approval action.
/// </summary>
public class ClientRegistrationResultDto
{
    public Guid Id { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public ClientRegistrationStatus RegistrationStatus { get; set; }

    public string? Secret { get; set; }

    public string? Message { get; set; }
}
