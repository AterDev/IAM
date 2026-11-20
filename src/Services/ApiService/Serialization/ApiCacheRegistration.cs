using Entity.IAMMod;
using IAMMod.Services;
using Microsoft.Extensions.Caching.Hybrid;
using ServiceDefaults;
using UserCenterMod.Managers;
using UserCenterMod.Models;

namespace ApiService.Serialization;

internal static class ApiCacheRegistration
{
    public static void AddIamCacheSerializers(this IServiceCollection services)
    {
        var json = IamCacheJsonContext.Default;
        services.AddSingleton<IHybridCacheSerializer<SigningKey>>(new JsonCacheSerializer<SigningKey>(json.SigningKey));
        services.AddSingleton<IHybridCacheSerializer<List<SigningKey>>>(new JsonCacheSerializer<List<SigningKey>>(json.ListSigningKey));
        services.AddSingleton<IHybridCacheSerializer<SessionValidationCacheEntry>>(new JsonCacheSerializer<SessionValidationCacheEntry>(json.SessionValidationCacheEntry));
        services.AddSingleton<IHybridCacheSerializer<UserCenterTokenUser>>(new JsonCacheSerializer<UserCenterTokenUser>(json.UserCenterTokenUser));
        services.AddSingleton<IHybridCacheSerializer<List<UserEntitlementDetailDto>>>(new JsonCacheSerializer<List<UserEntitlementDetailDto>>(json.ListUserEntitlementDetailDto));
    }
}
