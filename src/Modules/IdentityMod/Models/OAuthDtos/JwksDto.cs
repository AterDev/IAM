using System.Text.Json.Serialization;

namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// JSON Web Key Set
/// </summary>
/// <remarks>
/// Represents a set of JSON Web Keys as defined in RFC 7517.
/// Used to publish the public keys for JWT signature verification.
/// </remarks>
public class JwksDto
{
    /// <summary>
    /// Array of JSON Web Key values
    /// </summary>
    [JsonPropertyName("keys")]
    public required List<JsonWebKeyDto> Keys { get; set; }
}

/// <summary>
/// JSON Web Key
/// </summary>
/// <remarks>
/// Represents a single JSON Web Key as defined in RFC 7517.
/// Contains the public key information for JWT signature verification.
/// </remarks>
public class JsonWebKeyDto
{
    /// <summary>
    /// Key type (e.g., "RSA")
    /// </summary>
    [JsonPropertyName("kty")]
    public required string Kty { get; set; }

    /// <summary>
    /// Public key use (e.g., "sig" for signature)
    /// </summary>
    [JsonPropertyName("use")]
    public required string Use { get; set; }

    /// <summary>
    /// Key ID - unique identifier for the key
    /// </summary>
    [JsonPropertyName("kid")]
    public required string Kid { get; set; }

    /// <summary>
    /// Algorithm intended for use with the key (e.g., "RS256")
    /// </summary>
    [JsonPropertyName("alg")]
    public required string Alg { get; set; }

    /// <summary>
    /// RSA modulus (base64url encoded)
    /// </summary>
    [JsonPropertyName("n")]
    public string? N { get; set; }

    /// <summary>
    /// RSA public exponent (base64url encoded)
    /// </summary>
    [JsonPropertyName("e")]
    public string? E { get; set; }

    /// <summary>
    /// X.509 certificate chain (array of base64-encoded DER)
    /// </summary>
    [JsonPropertyName("x5c")]
    public List<string>? X5c { get; set; }

    /// <summary>
    /// X.509 certificate SHA-1 thumbprint (base64url encoded)
    /// </summary>
    [JsonPropertyName("x5t")]
    public string? X5t { get; set; }

    /// <summary>
    /// X.509 certificate SHA-256 thumbprint (base64url encoded)
    /// </summary>
    [JsonPropertyName("x5t#S256")]
    public string? X5tS256 { get; set; }
}
