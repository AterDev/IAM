using System.Text.Json.Serialization;

namespace ApiService.Models;

/// <summary>Success message returned by account and authorization operations.</summary>
public sealed record ApiMessageDto(string Message)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Count { get; init; }
}
