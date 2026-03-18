namespace IAMMod.Models.ClientDtos;

/// <summary>
/// Metadata for a previously issued client secret.
/// </summary>
public class ClientSecretHistoryDto
{
    public Guid Id { get; set; }

    public required string LastFour { get; set; }

    public DateTimeOffset IssuedTime { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive { get; set; }
}