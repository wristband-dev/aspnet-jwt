using System.Reflection;
using System.Security.Cryptography;

using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

using Moq;

namespace Wristband.AspNet.Auth.Jwt.Tests;

public class JwksProviderTests
{
    private const string Domain = "example.wristband.dev";
    private const string Kid = "test-kid";
    private const string FakeKeyJson = "{\"keys\":[{\"kty\":\"RSA\",\"kid\":\"test-kid\",\"n\":\"AQAB\",\"e\":\"AQAB\",\"use\":\"sig\",\"alg\":\"RS256\"}]}";

    private WristbandJwtValidationOptions Options => new() { WristbandApplicationDomain = Domain };

    private JwksProvider CreateProviderWithInjectedDependencies(
        out Mock<IJwksCache> mockCache,
        out Mock<IDocumentRetriever> mockRetriever)
    {
        mockCache = new Mock<IJwksCache>();
        mockRetriever = new Mock<IDocumentRetriever>();

        var provider = new JwksProvider(Options);

        var docRetrieverField = typeof(JwksProvider).GetField("_documentRetriever", BindingFlags.NonPublic | BindingFlags.Instance);
        docRetrieverField?.SetValue(provider, mockRetriever.Object);

        var keyRetrieverField = typeof(JwksProvider).GetField("_keySetRetriever", BindingFlags.NonPublic | BindingFlags.Instance);
        keyRetrieverField?.SetValue(provider, new JsonWebKeySetRetriever());

        var cacheField = typeof(JwksProvider).GetField("_jwksCache", BindingFlags.NonPublic | BindingFlags.Instance);
        cacheField?.SetValue(provider, mockCache.Object);

        return provider;
    }

    [Fact]
    public void Throws_When_Domain_Is_Missing()
    {
        var options = new WristbandJwtValidationOptions();
        Assert.Throws<ArgumentException>(() => new JwksProvider(options));
    }

    [Fact]
    public void Returns_Expected_ValidationParameters()
    {
        var provider = new JwksProvider(Options);
        var parameters = provider.GetTokenValidationParameters();

        Assert.True(parameters.ValidateIssuer);
        Assert.Equal($"https://{Domain}", parameters.ValidIssuer);
        Assert.Contains("RS256", parameters.ValidAlgorithms);
        Assert.True(parameters.ValidateIssuerSigningKey);
        Assert.NotNull(parameters.IssuerSigningKeyResolver);
    }

    [Fact]
    public void Resolver_Throws_If_Kid_Is_Missing()
    {
        var provider = new JwksProvider(Options);
        var parameters = provider.GetTokenValidationParameters();

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
            parameters.IssuerSigningKeyResolver("token", null, null, parameters));
    }

    [Fact]
    public void Resolver_Returns_Cached_Key_If_Found()
    {
        var mockCache = new Mock<IJwksCache>();
        var mockRetriever = new Mock<IDocumentRetriever>();

        var provider = CreateProviderWithInjectedDependencies(out mockCache, out mockRetriever);

        var mockKey = new RsaSecurityKey(RSA.Create()) { KeyId = Kid };
        SecurityKey outKey = mockKey;

        mockCache
            .Setup(x => x.TryGetKey(Kid, out outKey))
            .Returns(true);

        mockRetriever.Setup(r => r.GetDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("some-valid-document");

        var parameters = provider.GetTokenValidationParameters();
        var result = parameters.IssuerSigningKeyResolver("token", null, Kid, parameters);

        // Assert
        Assert.Single(result);
        Assert.Equal(mockKey, result.Single());
    }

    [Fact]
    public void Resolver_Parses_Jwks_And_Caches_Matching_Key()
    {
        var provider = CreateProviderWithInjectedDependencies(out var mockCache, out var mockRetriever);
        mockCache.Setup(x => x.TryGetKey(Kid, out It.Ref<SecurityKey>.IsAny)).Returns(false);
        mockRetriever.Setup(r => r.GetDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeKeyJson);

        var parameters = provider.GetTokenValidationParameters();
        var result = parameters.IssuerSigningKeyResolver("token", null, Kid, parameters);

        var key = result.Single();
        Assert.Equal(Kid, key.KeyId);
        mockCache.Verify(x => x.AddOrUpdate(Kid, key), Times.Once);
    }

    [Fact]
    public void Resolver_Throws_If_Jwks_Missing_Key()
    {
        var badJwks = "{\"keys\":[]}";
        var provider = CreateProviderWithInjectedDependencies(out var mockCache, out var mockRetriever);
        mockCache.Setup(x => x.TryGetKey(Kid, out It.Ref<SecurityKey>.IsAny)).Returns(false);
        mockRetriever.Setup(r => r.GetDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(badJwks);

        var parameters = provider.GetTokenValidationParameters();

        var ex = Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
            parameters.IssuerSigningKeyResolver("token", null, Kid, parameters));

        Assert.Contains("No key found", ex.Message);
    }

    [Fact]
    public void Resolver_Throws_If_DocumentRetriever_Fails()
    {
        var provider = CreateProviderWithInjectedDependencies(out var mockCache, out var mockRetriever);
        mockCache.Setup(x => x.TryGetKey(Kid, out It.Ref<SecurityKey>.IsAny)).Returns(false);
        mockRetriever.Setup(r => r.GetDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("network error"));

        var parameters = provider.GetTokenValidationParameters();

        var ex = Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
            parameters.IssuerSigningKeyResolver("token", null, Kid, parameters));

        Assert.Contains("Unable to retrieve JWKS", ex.Message);
        Assert.Contains("network error", ex?.InnerException?.Message);
    }

    [Fact]
    public void Resolver_Throws_If_Kid_Does_Not_Match()
    {
        var invalidKid = "invalid-kid";
        var provider = CreateProviderWithInjectedDependencies(out var mockCache, out var mockRetriever);
        mockCache.Setup(x => x.TryGetKey(Kid, out It.Ref<SecurityKey>.IsAny)).Returns(false);
        mockRetriever.Setup(r => r.GetDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeKeyJson);

        var parameters = provider.GetTokenValidationParameters();
        var ex = Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
            parameters.IssuerSigningKeyResolver("token", null, invalidKid, parameters));

        Assert.Contains("No key found", ex.Message);
    }

    [Fact]
    public void Resolver_Throws_If_Jwks_Request_Fails()
    {
        var provider = CreateProviderWithInjectedDependencies(out var mockCache, out var mockRetriever);
        mockCache.Setup(x => x.TryGetKey(Kid, out It.Ref<SecurityKey>.IsAny)).Returns(false);
        mockRetriever.Setup(r => r.GetDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("HTTP request failed"));

        var parameters = provider.GetTokenValidationParameters();
        var ex = Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
            parameters.IssuerSigningKeyResolver("token", null, Kid, parameters));

        Assert.Contains("Unable to retrieve JWKS", ex.Message);
        Assert.Contains("HTTP request failed", ex?.InnerException?.Message);
    }

    [Fact]
    public void Resolver_Throws_If_Jwks_Empty_But_Token_Valid()
    {
        var emptyJwks = "{\"keys\":[]}";
        var provider = CreateProviderWithInjectedDependencies(out var mockCache, out var mockRetriever);
        mockCache.Setup(x => x.TryGetKey(Kid, out It.Ref<SecurityKey>.IsAny)).Returns(false);
        mockRetriever.Setup(r => r.GetDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyJwks);

        var parameters = provider.GetTokenValidationParameters();
        var ex = Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
            parameters.IssuerSigningKeyResolver("token", null, Kid, parameters));

        Assert.Contains("No key found", ex.Message);
    }
}
