using Entity.IAMMod;
using IAMMod.Services;
using System.Text.Json.Serialization;
using UserCenterMod.Managers;
using UserCenterMod.Models;

namespace ApiService.Serialization;

[JsonSerializable(typeof(SigningKey))]
[JsonSerializable(typeof(List<SigningKey>))]
[JsonSerializable(typeof(SessionValidationCacheEntry))]
[JsonSerializable(typeof(UserCenterTokenUser))]
[JsonSerializable(typeof(List<UserEntitlementDetailDto>))]
internal partial class IamCacheJsonContext : JsonSerializerContext;
