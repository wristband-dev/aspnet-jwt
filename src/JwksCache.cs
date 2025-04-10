using System.Collections.Concurrent;

using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// LRU cache for JSON Web Keys that stores keys up to a maximum size limit
/// and optionally expires them after a configured TTL.
/// </summary>
internal class JwksCache
{
    private const int DefaultCacheSize = 3;
    private const int MaxCacheSize = 20;

    private readonly ConcurrentDictionary<string, JwksCacheEntry> _keyCache = new();
    private readonly LinkedList<string> _accessOrder = new();
    private readonly Dictionary<string, LinkedListNode<string>> _accessOrderNodes = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly int _maxSize;
    private readonly TimeSpan? _ttl;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwksCache"/> class.
    /// </summary>
    /// <param name="maxSize">Optional maximum number of keys to cache before using LRU eviction.</param>
    /// <param name="ttl">Optional TTL for cache entries. If null, entries never expire.</param>
    public JwksCache(int? maxSize, TimeSpan? ttl)
    {
        _maxSize = maxSize.HasValue && maxSize.Value > 0 && maxSize.Value <= MaxCacheSize
            ? maxSize.Value
            : DefaultCacheSize;
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
        if (_keyCache.TryGetValue(kid, out var entry))
        {
            // Check if entry is expired
            if (_ttl.HasValue && (DateTime.UtcNow - entry.LastAccessed) > _ttl.Value)
            {
                // Need to remove the expired entry
                _lock.EnterWriteLock();
                try
                {
                    // Check again inside the lock to avoid race conditions
                    if (_keyCache.TryGetValue(kid, out entry) &&
                        _ttl.HasValue && (DateTime.UtcNow - entry.LastAccessed) > _ttl.Value)
                    {
                        _keyCache.TryRemove(kid, out _);
                        RemoveFromAccessOrder(kid);
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                key = null!;
                return false;
            }

            // Update access time and order - Need write lock for this
            _lock.EnterWriteLock();
            try
            {
                // Get the latest entry again inside the lock
                if (_keyCache.TryGetValue(kid, out entry))
                {
                    // Update the LastAccessed time
                    entry.LastAccessed = DateTime.UtcNow;
                    _keyCache[kid] = entry;

                    // Update access order
                    if (_accessOrderNodes.TryGetValue(kid, out var node))
                    {
                        _accessOrder.Remove(node);
                    }

                    var newNode = _accessOrder.AddLast(kid);
                    _accessOrderNodes[kid] = newNode;

                    key = entry.Key;
                    return true;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        key = null!;
        return false;
    }

    /// <summary>
    /// Adds or updates a key in the cache, potentially evicting the least recently used key
    /// if the cache is at its maximum size.
    /// </summary>
    /// <param name="kid">The key ID.</param>
    /// <param name="key">The security key to cache.</param>
    public void AddOrUpdate(string kid, SecurityKey key)
    {
        var entry = new JwksCacheEntry { Key = key, LastAccessed = DateTime.UtcNow };

        _lock.EnterWriteLock();
        try
        {
            // Add or update the key
            _keyCache[kid] = entry;

            // Update access order
            if (_accessOrderNodes.TryGetValue(kid, out var existingNode))
            {
                _accessOrder.Remove(existingNode);
            }

            var node = _accessOrder.AddLast(kid);
            _accessOrderNodes[kid] = node;

            // Evict least recently used if over max size
            while (_keyCache.Count > _maxSize && _accessOrder.Count > 0)
            {
                var oldestNode = _accessOrder.First;
                if (oldestNode != null)
                {
                    var oldestKid = oldestNode.Value;
                    _accessOrder.RemoveFirst();
                    _accessOrderNodes.Remove(oldestKid);
                    _keyCache.TryRemove(oldestKid, out _);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Add this helper method
    private void RemoveFromAccessOrder(string kid)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_accessOrderNodes.TryGetValue(kid, out var node))
            {
                _accessOrder.Remove(node);
                _accessOrderNodes.Remove(kid);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
