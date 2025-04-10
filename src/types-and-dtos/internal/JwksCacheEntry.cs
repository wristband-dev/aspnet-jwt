using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Cache entry that tracks a JWK and its last access time.
/// </summary>
internal class JwksCacheEntry
{
    /// <summary>
    /// Gets or sets the security key.
    /// </summary>
    public SecurityKey Key { get; set; } = null!;

    /// <summary>
    /// Gets or sets the last time this entry was accessed.
    /// </summary>
    public DateTime LastAccessed { get; set; }
}
