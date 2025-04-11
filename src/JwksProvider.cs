using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// The <see cref="JwksProvider"/> class is responsible for retrieving and managing JSON Web Key Sets (JWKS)
/// from your Wristband application's domain. It provides the necessary token validation parameters and event handling
/// to validate JWTs issued by Wristband. The JWKS is used to resolve signing keys for token validation.
/// </summary>
internal class JwksProvider : IJwksProvider
{
    private const string JwksApiPath = "/api/v1/oauth2/jwks";

    private readonly string _jwksUri;
    private readonly string _issuerDomain;
    private readonly JsonWebKeySetRetriever _keySetRetriever;
    private readonly IDocumentRetriever _documentRetriever;
    private readonly IJwksCache _jwksCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwksProvider"/> class with configurable dependencies for testing and customization.
    /// </summary>
    /// <param name="options">Options containing the Wristband domain and JWKS cache settings.</param>
    /// <param name="documentRetriever">Optional document retriever for fetching the JWKS endpoint.</param>
    /// <param name="keySetRetriever">Optional key set retriever for parsing the JWKS response.</param>
    /// <param name="jwksCache">Optional cache for storing individual keys by 'kid'.</param>
    public JwksProvider(
        WristbandJwtValidationOptions options,
        IDocumentRetriever? documentRetriever = null,
        JsonWebKeySetRetriever? keySetRetriever = null,
        IJwksCache? jwksCache = null)
    {
        if (string.IsNullOrEmpty(options.WristbandApplicationDomain))
        {
            throw new ArgumentException("WristbandApplicationDomain must be set in options.", nameof(options));
        }

        _issuerDomain = $"https://{options.WristbandApplicationDomain}";
        _jwksUri = $"{_issuerDomain}{JwksApiPath}";
        _documentRetriever = documentRetriever ?? new HttpDocumentRetriever { RequireHttps = true };
        _keySetRetriever = keySetRetriever ?? new JsonWebKeySetRetriever();
        _jwksCache = jwksCache ?? new JwksCache(options.JwksCacheMaxSize, options.JwksCacheTtl);
    }

    /// <summary>
    /// Constructs <see cref="TokenValidationParameters"/> that can be used to validate JWTs from Wristband.
    /// Configures issuer, algorithm, and signing key resolution logic including key caching.
    /// </summary>
    /// <returns>A configured <see cref="TokenValidationParameters"/> instance.</returns>
    public TokenValidationParameters GetTokenValidationParameters()
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
                if (string.IsNullOrEmpty(kid))
                {
                    throw new SecurityTokenSignatureKeyNotFoundException("No key ID (kid) found in the token");
                }

                if (_jwksCache.TryGetKey(kid, out var cachedKey))
                {
                    return new[] { cachedKey };
                }

                try
                {
                    var jwks = _keySetRetriever.GetConfigurationAsync(
                        _jwksUri,
                        _documentRetriever,
                        CancellationToken.None).GetAwaiter().GetResult();

                    var matchingKey = jwks.Keys.FirstOrDefault(k => k.Kid == kid);
                    if (matchingKey != null)
                    {
                        _jwksCache.AddOrUpdate(kid, matchingKey);
                        return new[] { matchingKey };
                    }
                }
                catch (Exception ex)
                {
                    throw new SecurityTokenSignatureKeyNotFoundException(
                        $"Unable to retrieve JWKS from {_jwksUri}: {ex.Message}",
                        ex);
                }

                throw new SecurityTokenSignatureKeyNotFoundException($"No key found in JWKS matching kid: {kid}");
            },
        };
    }
}
