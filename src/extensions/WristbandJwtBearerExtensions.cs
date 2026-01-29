using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Extension methods for configuring Wristband JWT Bearer authentication.
/// </summary>
public static class WristbandJwtBearerExtensions
{
    /// <summary>
    /// Configures JWT Bearer authentication to use Wristband JWKS validation.
    /// This extension method sets up JSON Web Key Set (JWKS) retrieval and token validation
    /// using signing keys from your Wristband application domain.
    /// </summary>
    /// <param name="options">The JWT Bearer options to configure.</param>
    /// <param name="wristbandApplicationVanityDomain">
    /// The Wristband application vanity domain (e.g., "invotastic.us.wristband.dev").
    /// </param>
    /// <param name="jwksCacheMaxSize">
    /// Optional maximum number of JWKs to cache in memory. Defaults to 20.
    /// When exceeded, the least recently used keys are evicted.
    /// </param>
    /// <param name="jwksCacheTtl">
    /// Optional time-to-live for cached JWKs. If not set, keys remain in cache until eviction by size limit.
    /// </param>
    /// <returns>The JWT Bearer options for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when wristbandApplicationVanityDomain is null or empty.</exception>
    /// <example>
    /// <code>
    /// builder.Services.AddAuthentication()
    ///     .AddJwtBearer(options => options.UseWristbandJwksValidation(
    ///         wristbandApplicationVanityDomain: "invotastic.us.wristband.dev",
    ///         jwksCacheMaxSize: 10,
    ///         jwksCacheTtl: TimeSpan.FromHours(12)
    ///     ));
    /// </code>
    /// </example>
    public static JwtBearerOptions UseWristbandJwksValidation(
        this JwtBearerOptions options,
        string wristbandApplicationVanityDomain,
        int? jwksCacheMaxSize = null,
        TimeSpan? jwksCacheTtl = null)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrEmpty(wristbandApplicationVanityDomain))
        {
            throw new ArgumentException(
                "Wristband application vanity domain is required",
                nameof(wristbandApplicationVanityDomain));
        }

        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = wristbandApplicationVanityDomain,
            JwksCacheMaxSize = jwksCacheMaxSize ?? 20,
            JwksCacheTtl = jwksCacheTtl,
        };

        var jwksProvider = new JwksProvider(config);
        options.TokenValidationParameters = jwksProvider.GetTokenValidationParameters();

        // Preserve original JWT claim names
        options.MapInboundClaims = false;

        return options;
    }
}
