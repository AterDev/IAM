using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace Share.Services;

/// <summary>
/// Key management service implementation
/// </summary>
public class KeyManagementService : IKeyManagementService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<KeyManagementService> _logger;
    private const string CurrentKeyIdCacheKey = "CurrentSigningKeyId";
    private const string SigningCredentialsCacheKey = "SigningCredentials";
    private const string ValidationParametersCacheKey = "ValidationParameters";
    private const int KeyCacheDurationMinutes = 60;

    // In-memory key storage (in production, this should be backed by database/SigningKey entity)
    private static RSA? _currentKey;
    private static string? _currentKeyId;
    private static readonly object _lock = new();

    public KeyManagementService(IMemoryCache cache, ILogger<KeyManagementService> logger)
    {
        _cache = cache;
        _logger = logger;
        InitializeKeyIfNeeded();
    }

    public SigningCredentials GetSigningCredentials()
    {
        return _cache.GetOrCreate(SigningCredentialsCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(KeyCacheDurationMinutes);
            
            lock (_lock)
            {
                InitializeKeyIfNeeded();
                
                var rsaKey = new RsaSecurityKey(_currentKey!)
                {
                    KeyId = _currentKeyId
                };
                
                return new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);
            }
        })!;
    }

    public TokenValidationParameters GetTokenValidationParameters()
    {
        return _cache.GetOrCreate(ValidationParametersCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(KeyCacheDurationMinutes);
            
            lock (_lock)
            {
                InitializeKeyIfNeeded();
                
                var rsaKey = new RsaSecurityKey(_currentKey!)
                {
                    KeyId = _currentKeyId
                };
                
                return new TokenValidationParameters
                {
                    IssuerSigningKey = rsaKey,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            }
        })!;
    }

    public Task<string> RotateKeyAsync()
    {
        lock (_lock)
        {
            _logger.LogInformation("Rotating signing key");
            
            // Generate new key
            _currentKey?.Dispose();
            _currentKey = RSA.Create(2048);
            _currentKeyId = Guid.NewGuid().ToString("N");
            
            // Clear cache to force refresh
            _cache.Remove(SigningCredentialsCacheKey);
            _cache.Remove(ValidationParametersCacheKey);
            _cache.Remove(CurrentKeyIdCacheKey);
            
            _logger.LogInformation("New signing key generated with ID: {KeyId}", _currentKeyId);
            
            // TODO: In production, save to database via SigningKey entity
            
            return Task.FromResult(_currentKeyId);
        }
    }

    public string? GetCurrentKeyId()
    {
        return _cache.GetOrCreate(CurrentKeyIdCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(KeyCacheDurationMinutes);
            
            lock (_lock)
            {
                InitializeKeyIfNeeded();
                return _currentKeyId;
            }
        });
    }

    public Task<string?> GetPublicKeyJwkAsync(string? keyId = null)
    {
        lock (_lock)
        {
            InitializeKeyIfNeeded();
            
            if (keyId != null && keyId != _currentKeyId)
            {
                _logger.LogWarning("Requested key ID {RequestedKeyId} does not match current key ID {CurrentKeyId}", keyId, _currentKeyId);
                return Task.FromResult<string?>(null);
            }
            
            var parameters = _currentKey!.ExportParameters(false);
            
            // Create JWK representation
            var jwk = new
            {
                kty = "RSA",
                use = "sig",
                kid = _currentKeyId,
                alg = "RS256",
                n = Base64UrlEncoder.Encode(parameters.Modulus!),
                e = Base64UrlEncoder.Encode(parameters.Exponent!)
            };
            
            var jwkJson = System.Text.Json.JsonSerializer.Serialize(jwk);
            return Task.FromResult<string?>(jwkJson);
        }
    }

    private void InitializeKeyIfNeeded()
    {
        if (_currentKey == null || _currentKeyId == null)
        {
            lock (_lock)
            {
                if (_currentKey == null || _currentKeyId == null)
                {
                    _logger.LogInformation("Initializing signing key");
                    _currentKey = RSA.Create(2048);
                    _currentKeyId = Guid.NewGuid().ToString("N");
                    
                    // TODO: In production, load from database via SigningKey entity
                    // or create and save if doesn't exist
                }
            }
        }
    }
}
