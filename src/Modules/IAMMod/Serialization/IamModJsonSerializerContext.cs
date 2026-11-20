using System.Text.Json;
using System.Text.Json.Serialization;
using Entity.IAMMod;
using IAMMod.Services;

namespace IAMMod.Serialization;

/// <summary>
/// Source-generated metadata for IAM module payloads persisted in token and audit records.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(AuthorizationCodeProperties))]
[JsonSerializable(typeof(AuthorizationCodeTokenPayload))]
[JsonSerializable(typeof(MfaTotpSecretPayload))]
[JsonSerializable(typeof(List<MfaRecoveryCodeEntry>))]
[JsonSerializable(typeof(PasswordResetTokenPayload))]
[JsonSerializable(typeof(PermissionSeedDefinition))]
[JsonSerializable(typeof(List<PermissionSeedDefinition>))]
[JsonSerializable(typeof(RiskControlOption))]
[JsonSerializable(typeof(LoginRiskAssessment))]
[JsonSerializable(typeof(LoginFailureTrackingResult))]
[JsonSerializable(typeof(DevicePollingAssessment))]
[JsonSerializable(typeof(IamAuditPayload))]
internal partial class IamModJsonSerializerContext : JsonSerializerContext;

/// <summary>
/// AOT-safe envelope for manager audit events. Optional members are omitted so
/// each event keeps the same compact shape as its former anonymous payload.
/// </summary>
public sealed class IamAuditPayload
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? UserName { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Provider { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Email { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? PhoneNumber { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ClientId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ClientIdentifier { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? DisplayName { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? DeviceCode { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Reason { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? TokenTypeHint { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? TokenType { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? PreviousRefreshToken { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? NewRefreshToken { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? RefreshToken { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? SessionId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? LastKnownIpAddress { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? LastKnownUserAgent { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? LockoutEnd { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Guid? AuthorizationId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Guid? RequestingClientId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Guid? DeveloperUserId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Guid? ConflictingUserId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public DateTimeOffset? ChangedAt { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public DateTimeOffset? BlockedUntil { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public DateTimeOffset? SecretExpiresAt { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? AttemptCount { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? IpFailureCount { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? FailedCount { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? Count { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? HasKnownIp { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? HasKnownUserAgent { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? RequiresStepUp { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? Created { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ClientType? Type { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ApplicationType? ApplicationType { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ClientRegistrationStatus? RegistrationStatus { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<Guid>? Added { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<Guid>? Removed { get; set; }
}

internal static class IamJson
{
    public static string Serialize(IamAuditPayload payload) =>
        JsonSerializer.Serialize(payload, IamModJsonSerializerContext.Default.IamAuditPayload);

    public static string Serialize(Dictionary<string, string> payload) =>
        JsonSerializer.Serialize(payload, IamModJsonSerializerContext.Default.DictionaryStringString);

    public static Dictionary<string, string>? DeserializeDictionary(string json) =>
        JsonSerializer.Deserialize(json, IamModJsonSerializerContext.Default.DictionaryStringString);

    public static PasswordResetTokenPayload? DeserializePasswordReset(string json) =>
        JsonSerializer.Deserialize(json, IamModJsonSerializerContext.Default.PasswordResetTokenPayload);

    public static string Serialize(PasswordResetTokenPayload payload) =>
        JsonSerializer.Serialize(payload, IamModJsonSerializerContext.Default.PasswordResetTokenPayload);
}

/// <summary>Properties persisted with an authorization-code grant.</summary>
public sealed record AuthorizationCodeProperties(
    [property: JsonPropertyName("redirect_uri")] string RedirectUri,
    [property: JsonPropertyName("code_challenge")] string? CodeChallenge,
    [property: JsonPropertyName("code_challenge_method")] string? CodeChallengeMethod,
    [property: JsonPropertyName("nonce")] string? Nonce,
    [property: JsonPropertyName("sid")] string? SessionId);

/// <summary>Payload persisted with an authorization-code token.</summary>
public sealed record AuthorizationCodeTokenPayload(
    [property: JsonPropertyName("authorization_id")] Guid AuthorizationId);

/// <summary>Persisted TOTP secret metadata.</summary>
public sealed record MfaTotpSecretPayload(
    string Secret,
    string Issuer,
    string AccountName,
    DateTimeOffset CreatedAt);

/// <summary>Hashed MFA recovery-code metadata.</summary>
public sealed class MfaRecoveryCodeEntry
{
    public MfaRecoveryCodeEntry()
    {
    }

    public MfaRecoveryCodeEntry(string hash, string salt, DateTimeOffset? usedAt, string suffix)
    {
        Hash = hash;
        Salt = salt;
        UsedAt = usedAt;
        Suffix = suffix;
    }

    public string Hash { get; set; } = string.Empty;

    public string Salt { get; set; } = string.Empty;

    public DateTimeOffset? UsedAt { get; set; }

    public string Suffix { get; set; } = string.Empty;
}

/// <summary>Persisted password-reset token metadata.</summary>
public sealed record PasswordResetTokenPayload(
    string Hash,
    string Salt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConsumedAt,
    string? SecurityStamp);
