namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Provides configuration options for Wristband JWT validation.
/// </summary>
public class WristbandJwtValidationOptions
{
    /// <summary>
    /// Gets or sets the Wristband application domain. This value is used to construct the
    /// JWKS endpoint URL for token validation. Example: "myapp.wristband.dev".
    /// </summary>
    public string? WristbandApplicationDomain { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of JWK keys to cache. When this limit is reached, the least recently
    /// used keys will be evicted from the cache. Default is 3.
    /// </summary>
    public int? JwksCacheMaxSize { get; set; }

    /// <summary>
    /// Gets or sets the time-to-live for cached JWK keys.
    /// If null (the default), keys are cached indefinitely until evicted due to the cache size limit.
    /// </summary>
    public TimeSpan? JwksCacheTtl { get; set; }
}
