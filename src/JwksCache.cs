using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Cache for JSON Web Keys using the built-in MemoryCache.
/// </summary>
internal class JwksCache : IJwksCache
{
    private const int DefaultMaxCacheSize = 20;
    private readonly MemoryCache _cache;
    private readonly TimeSpan? _ttl;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwksCache"/> class.
    /// </summary>
    /// <param name="maxSize">Optional maximum number of keys to cache before using LRU eviction.</param>
    /// <param name="ttl">Optional TTL for cache entries. If null, entries never expire.</param>
    public JwksCache(int? maxSize, TimeSpan? ttl)
    {
        var size = maxSize.HasValue && maxSize.Value > 0 ? maxSize.Value : DefaultMaxCacheSize;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = size,
        });
        _ttl = ttl;
    }

    /// <summary>
    /// Tries to get a key from the cache.
    /// </summary>
    /// <param name="kid">The key ID to look up.</param>
    /// <param name="key">The output security key if found.</param>
    /// <returns>True if the key was found, false otherwise.</returns>
    public bool TryGetKey(string kid, out SecurityKey key)
    {
        if (_cache.TryGetValue(kid, out SecurityKey? cachedKey) && cachedKey != null)
        {
            key = cachedKey;
            return true;
        }

        key = null!;
        return false;
    }

    /// <summary>
    /// Adds or updates a key in the cache.
    /// </summary>
    /// <param name="kid">The key ID.</param>
    /// <param name="key">The security key to cache.</param>
    public void AddOrUpdate(string kid, SecurityKey key)
    {
        // Each key counts as 1 toward the size limit
        var options = new MemoryCacheEntryOptions
        {
            Size = 1,
        };

        if (_ttl.HasValue)
        {
            options.SlidingExpiration = _ttl.Value;
        }

        _cache.Set(kid, key, options);
    }
}
