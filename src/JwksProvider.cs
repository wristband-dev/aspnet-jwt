using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// The <see cref="JwksProvider"/> class is responsible for retrieving and managing JSON Web Key Sets (JWKS)
/// from your Wristband application's domain. It provides the necessary token validation parameters and event handling
/// to validate JWTs issued by Wristband. The JWKS is used to resolve signing keys for token validation.
/// </summary>
internal class JwksProvider
{
    private const string JwksApiPath = "/api/v1/oauth2/jwks";

    private readonly string _jwksUri;
    private readonly string _issuerDomain;
    private readonly JsonWebKeySetRetriever _keySetRetriever;
    private readonly HttpDocumentRetriever _documentRetriever;
    private readonly JwksCache _jwksCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwksProvider"/> class. Configures the JWKS provider by setting the
    /// issuer domain and initializing the JWKS configuration manager to handle key retrieval and caching.
    /// </summary>
    /// <param name="options">
    /// The <see cref="WristbandJwtValidationOptions"/> used to configure the JWKS provider, including the application
    /// domain for the issuer and the JWKS URI.
    /// </param>
    internal JwksProvider(WristbandJwtValidationOptions options)
    {
        if (string.IsNullOrEmpty(options.WristbandApplicationDomain))
        {
            throw new ArgumentException("WristbandApplicationDomain must be set in options.", nameof(options));
        }

        _issuerDomain = $"https://{options.WristbandApplicationDomain}";
        _jwksUri = $"{_issuerDomain}{JwksApiPath}";
        _keySetRetriever = new JsonWebKeySetRetriever();
        _documentRetriever = new HttpDocumentRetriever { RequireHttps = true };
        _jwksCache = new JwksCache(options.JwksCacheMaxSize, options.JwksCacheTtl);
    }

    /// <summary>
    /// Generates <see cref="TokenValidationParameters"/> based on the JWKS configuration used to validate the JWT
    /// token during authentication.
    /// </summary>
    /// <returns>
    /// Returns <see cref="TokenValidationParameters"/> with settings such as issuer validation, algorithm, and signing
    /// key resolver.
    /// </returns>
    internal TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuerDomain,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = new[] { "RS256" },
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                // Handle case where no kid is provided in the token.
                if (string.IsNullOrEmpty(kid))
                {
                    throw new SecurityTokenSignatureKeyNotFoundException("No key ID (kid) found in the token");
                }

                // Try to get the key from cache first.
                if (_jwksCache.TryGetKey(kid, out var cachedKey))
                {
                    return new[] { cachedKey };
                }

                // If not in cache, get the full JWKS.
                try
                {
                    var jwks = _keySetRetriever.GetConfigurationAsync(
                        _jwksUri,
                        _documentRetriever,
                        CancellationToken.None).GetAwaiter().GetResult();

                    // Look for the key matching the kid
                    var matchingKey = jwks.Keys.FirstOrDefault(k => k.Kid == kid);
                    if (matchingKey != null)
                    {
                        // Only cache the matching key
                        _jwksCache.AddOrUpdate(kid, matchingKey);
                        return new[] { matchingKey };
                    }
                }
                catch (Exception ex)
                {
                    // If all else fails, throw the exception
                    throw new SecurityTokenSignatureKeyNotFoundException(
                        $"Unable to retrieve JWKS from {_jwksUri}: {ex.Message}",
                        ex);
                }

                // Throw if no matching key was found.
                throw new SecurityTokenSignatureKeyNotFoundException($"No key found in JWKS matching kid: {kid}");
            },
        };
    }
}
