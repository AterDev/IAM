using System.Text.Json.Serialization;

namespace Share.Models.Auth;
/// <summary>
/// 刷新 token
/// </summary>
public class TokenRefreshDto
{
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}
