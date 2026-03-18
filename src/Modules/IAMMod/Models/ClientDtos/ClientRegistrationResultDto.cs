namespace IAMMod.Models.ClientDtos;

/// <summary>
/// Result of a client self-service registration or approval action.
/// </summary>
public class ClientRegistrationResultDto
{
    public Guid Id { get; set; }

    public required string ClientId { get; set; }

    public ClientRegistrationStatus RegistrationStatus { get; set; }

    public string? Secret { get; set; }

    public string? Message { get; set; }
}