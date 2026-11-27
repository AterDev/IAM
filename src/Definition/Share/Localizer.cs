using Microsoft.Extensions.Localization;

namespace Share;

/// <summary>
/// 本地化资源
/// </summary>
public partial class Localizer(IStringLocalizer<Localizer> localizer)
{
    public string Get(string key, params object[] arguments)
    {
        var localized = localizer[key, arguments];
        return localized.ResourceNotFound || string.IsNullOrWhiteSpace(localized.Value)
            ? key
            : localized.Value;
    }
}
