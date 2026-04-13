using Microsoft.Extensions.Localization;

namespace Share;

/// <summary>
/// 本地化资源
/// </summary>
public partial class Localizer(IStringLocalizer<Localizer> localizer)
{
    public string Get(string key, params object[] arguments)
    {
        return string.IsNullOrWhiteSpace(localizer[key, arguments]) ? key : localizer[key, arguments];
    }
}
