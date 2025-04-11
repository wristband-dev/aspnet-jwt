using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt.Tests;

public class JwksCacheTests
{
    private readonly SecurityKey _testKey1;
    private readonly SecurityKey _testKey2;
    private readonly SecurityKey _testKey3;

    public JwksCacheTests()
    {
        _testKey1 = new SymmetricSecurityKey(new byte[32]) { KeyId = "key1" };
        _testKey2 = new SymmetricSecurityKey(new byte[32]) { KeyId = "key2" };
        _testKey3 = new SymmetricSecurityKey(new byte[32]) { KeyId = "key3" };
    }

    [Fact]
    public void Constructor_AcceptsNullMaxSizeParameter()
    {
        var cache = new JwksCache(null, null);

        cache.AddOrUpdate("kid1", _testKey1);
        Assert.True(cache.TryGetKey("kid1", out _));
    }

    [Fact]
    public void Constructor_AcceptsZeroMaxSizeParameter()
    {
        var cache = new JwksCache(0, null);

        // Just verify we can use the cache
        cache.AddOrUpdate("kid1", _testKey1);
        Assert.True(cache.TryGetKey("kid1", out _));
    }

    [Fact]
    public void Constructor_AcceptsNegativeMaxSizeParameter()
    {
        var cache = new JwksCache(-5, null);

        // Just verify we can use the cache
        cache.AddOrUpdate("kid1", _testKey1);
        Assert.True(cache.TryGetKey("kid1", out _));
    }

    [Fact]
    public void TryGetKey_WithExistingKey_ReturnsTrue()
    {
        var cache = new JwksCache(10, null);
        cache.AddOrUpdate("kid1", _testKey1);

        bool result = cache.TryGetKey("kid1", out var key);

        Assert.True(result);
        Assert.Same(_testKey1, key);
    }

    [Fact]
    public void TryGetKey_WithNonExistentKey_ReturnsFalse()
    {
        var cache = new JwksCache(10, null);
        bool result = cache.TryGetKey("nonexistent", out var key);

        Assert.False(result);
        Assert.Null(key);
    }

    [Fact]
    public void TryGetKey_AfterExpiration_ReturnsFalse()
    {
        // Create cache with very short TTL
        var cache = new JwksCache(10, TimeSpan.FromMilliseconds(50));
        cache.AddOrUpdate("kid1", _testKey1);

        // Wait for expiration
        Thread.Sleep(100);

        bool result = cache.TryGetKey("kid1", out var key);
        Assert.False(result);
        Assert.Null(key);
    }

    [Fact]
    public void TryGetKey_ResetsSlidingExpiration()
    {
        // Create cache with short TTL
        var cache = new JwksCache(10, TimeSpan.FromMilliseconds(150));
        cache.AddOrUpdate("kid1", _testKey1);

        // Wait some time but not enough to expire
        Thread.Sleep(100);

        // Access the key to reset the sliding window
        bool firstResult = cache.TryGetKey("kid1", out _);

        // Wait again - if sliding window wasn't reset, it would expire
        Thread.Sleep(100);

        // Try to get the key again
        bool secondResult = cache.TryGetKey("kid1", out var key);
        Assert.True(firstResult);
        Assert.True(secondResult);
        Assert.Same(_testKey1, key);
    }

    [Fact]
    public void AddOrUpdate_WithNewKey_AddsToCache()
    {
        var cache = new JwksCache(10, null);
        cache.AddOrUpdate("kid1", _testKey1);

        Assert.True(cache.TryGetKey("kid1", out var key));
        Assert.Same(_testKey1, key);
    }

    [Fact]
    public void AddOrUpdate_WithExistingKey_UpdatesCache()
    {
        var cache = new JwksCache(10, null);
        cache.AddOrUpdate("kid1", _testKey1);
        cache.AddOrUpdate("kid1", _testKey2);

        Assert.True(cache.TryGetKey("kid1", out var key));
        Assert.Same(_testKey2, key);
        Assert.NotSame(_testKey1, key);
    }

    [Fact]
    public void AddOrUpdate_WithTtl_SetsExpiration()
    {
        var ttl = TimeSpan.FromMilliseconds(50);
        var cache = new JwksCache(10, ttl);

        cache.AddOrUpdate("kid1", _testKey1);

        // Initial check should succeed
        bool initialResult = cache.TryGetKey("kid1", out _);

        // Wait for expiration
        Thread.Sleep(100);

        // Check after expiration
        bool finalResult = cache.TryGetKey("kid1", out _);
        Assert.True(initialResult);
        Assert.False(finalResult);
    }

    [Fact]
    public void Cache_CanStoreMultipleKeys()
    {
        var cache = new JwksCache(10, null);
        cache.AddOrUpdate("kid1", _testKey1);
        cache.AddOrUpdate("kid2", _testKey2);
        cache.AddOrUpdate("kid3", _testKey3);

        Assert.True(cache.TryGetKey("kid1", out var key1));
        Assert.True(cache.TryGetKey("kid2", out var key2));
        Assert.True(cache.TryGetKey("kid3", out var key3));
        Assert.Same(_testKey1, key1);
        Assert.Same(_testKey2, key2);
        Assert.Same(_testKey3, key3);
    }

    [Fact]
    public async Task MultipleThreads_ModifyingCache_DoesNotThrow()
    {
        var cache = new JwksCache(10, null);
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (int i = 0; i < 5; i++)
        {
            var threadNum = i;
            var task = Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 10; j++)
                    {
                        var keyId = $"key{threadNum}-{j}";
                        var key = new SymmetricSecurityKey(new byte[32]) { KeyId = keyId };
                        cache.AddOrUpdate(keyId, key);

                        // Try to read keys from other threads
                        for (int k = 0; k < 3; k++)
                        {
                            var otherThreadNum = (threadNum + k) % 5;
                            cache.TryGetKey($"key{otherThreadNum}-{j / 2}", out _);
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            tasks.Add(task);
        }

        // Wait for all tasks to complete asynchronously
        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
    }
}
