using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Provides an abstraction for caching and retrieving JSON Web Keys (JWK) used in JWT token validation.
/// </summary>
public interface IJwksCache
{
    /// <summary>
    /// Attempts to retrieve a security key from the cache using the provided key identifier.
    /// </summary>
    /// <param name="kid">The key identifier (kid) to look up.</param>
    /// <param name="key">When the method returns, contains the retrieved security key, or null if not found.</param>
    /// <returns>True if a key was found; otherwise, false.</returns>
    bool TryGetKey(string kid, out SecurityKey key);

    /// <summary>
    /// Adds a new security key to the cache or updates an existing key with the specified key identifier.
    /// </summary>
    /// <param name="kid">The key identifier (kid) to associate with the security key.</param>
    /// <param name="key">The security key to cache.</param>
    void AddOrUpdate(string kid, SecurityKey key);
}
