namespace Share.Models.Auth;
/// <summary>
/// 令牌信息
/// </summary>
public class AccessTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    /// <summary>
    /// 过期时间秒
    /// </summary>
    public int ExpiresIn { get; set; }

    public int RefreshExpiresIn { get; set; }
}
