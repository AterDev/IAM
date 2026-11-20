using System.Text.Json.Serialization;

namespace ApiService.Models;

/// <summary>OAuth protocol error payload emitted by the token and user-info endpoints.</summary>
public sealed class OAuthErrorDto
{
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    [JsonPropertyName("error_description")]
    public required string ErrorDescription { get; init; }
}
