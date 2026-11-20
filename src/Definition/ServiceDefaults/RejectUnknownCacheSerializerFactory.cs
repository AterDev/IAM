using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Hybrid;

namespace ServiceDefaults;

internal sealed class RejectUnknownCacheSerializerFactory : IHybridCacheSerializerFactory
{
    public bool TryCreateSerializer<T>([NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer)
    {
        serializer = null;
        if (typeof(T) == typeof(string) || typeof(T) == typeof(byte[]))
        {
            return false;
        }

        throw new NotSupportedException($"Register source-generated cache serialization for {typeof(T)}.");
    }
}
