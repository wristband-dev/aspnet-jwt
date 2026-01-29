using System.Reflection;

using Microsoft.AspNetCore.Http;

namespace Wristband.AspNet.Auth.Jwt.Tests;

public class WristbandJwtValidatorTests
{
    [Fact]
    public void Create_WithValidConfig_ReturnsValidator()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);
        Assert.NotNull(validator);
    }

    [Fact]
    public void Create_WithNullConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => WristbandJwtValidator.Create(null!));
    }

    [Fact]
    public void Create_WithEmptyDomain_ThrowsArgumentException()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = ""
        };
        var ex = Assert.Throws<ArgumentException>(() => WristbandJwtValidator.Create(config));
        Assert.Contains("WristbandApplicationVanityDomain", ex.Message);
    }

    [Fact]
    public void Create_WithCustomCacheSettings_AppliesSettings()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev",
            JwksCacheMaxSize = 50,
            JwksCacheTtl = TimeSpan.FromMinutes(30)
        };
        var validator = WristbandJwtValidator.Create(config);
        Assert.NotNull(validator);
    }

    [Fact]
    public void Create_WithNullDomain_ThrowsArgumentException()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = null!
        };
        var ex = Assert.Throws<ArgumentException>(() => WristbandJwtValidator.Create(config));
        Assert.Contains("WristbandApplicationVanityDomain", ex.Message);
    }

    [Fact]
    public void ExtractBearerToken_WithValidHeader_ReturnsToken()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);
        var authHeader = "Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.test.signature";
        var token = validator.ExtractBearerToken(authHeader);
        Assert.Equal("eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.test.signature", token);
    }

    [Fact]
    public void ExtractBearerToken_WithNullHeader_ThrowsArgumentException()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);
        var ex = Assert.Throws<ArgumentException>(() => validator.ExtractBearerToken(null));
        Assert.Contains("Authorization header is missing", ex.Message);
    }

    [Fact]
    public void ExtractBearerToken_WithEmptyHeader_ThrowsArgumentException()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);
        var ex = Assert.Throws<ArgumentException>(() => validator.ExtractBearerToken(""));
        Assert.Contains("Authorization header is missing", ex.Message);
    }

    [Fact]
    public void ExtractBearerToken_WithNonBearerScheme_ThrowsArgumentException()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);
        var ex = Assert.Throws<ArgumentException>(() => validator.ExtractBearerToken("Basic dXNlcjpwYXNz"));
        Assert.Contains("Bearer scheme", ex.Message);
    }

    [Fact]
    public void ExtractBearerToken_WithMissingToken_ThrowsArgumentException()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);
        var ex = Assert.Throws<ArgumentException>(() => validator.ExtractBearerToken("Bearer "));
        Assert.Contains("Bearer token value is missing", ex.Message);
    }

    [Fact]
    public void ExtractBearerToken_WithWhitespaceHeader_ThrowsArgumentException()
    {
        var config = new WristbandJwtValidatorConfig { WristbandApplicationVanityDomain = "test.wristband.dev" };
        var validator = WristbandJwtValidator.Create(config);
        var ex = Assert.Throws<ArgumentException>(() => validator.ExtractBearerToken("   "));
        Assert.Contains("Authorization header is missing", ex.Message);
    }

    [Fact]
    public void ExtractBearerToken_WithCaseInsensitiveBearer_ReturnsToken()
    {
        var config = new WristbandJwtValidatorConfig { WristbandApplicationVanityDomain = "test.wristband.dev" };
        var validator = WristbandJwtValidator.Create(config);
        var token = validator.ExtractBearerToken("bearer test-token");
        Assert.Equal("test-token", token);
    }

    [Fact]
    public void ExtractBearerToken_WithExtraWhitespace_ReturnsTrimmedToken()
    {
        var config = new WristbandJwtValidatorConfig { WristbandApplicationVanityDomain = "test.wristband.dev" };
        var validator = WristbandJwtValidator.Create(config);
        var token = validator.ExtractBearerToken("Bearer   test-token   ");
        Assert.Equal("test-token", token);
    }

    [Fact]
    public void ExtractBearerToken_WithOnlyWhitespaceToken_ThrowsArgumentException()
    {
        var config = new WristbandJwtValidatorConfig { WristbandApplicationVanityDomain = "test.wristband.dev" };
        var validator = WristbandJwtValidator.Create(config);
        var ex = Assert.Throws<ArgumentException>(() => validator.ExtractBearerToken("Bearer    "));
        Assert.Contains("Bearer token value is missing", ex.Message);
    }

    [Fact]
    public async Task Validate_WithValidToken_ReturnsSuccessWithPayload()
    {
        var domain = "test.wristband.dev";
        var issuer = $"https://{domain}";
        var config = new WristbandJwtValidatorConfig { WristbandApplicationVanityDomain = domain };
        var validator = WristbandJwtValidator.Create(config);

        var mockProvider = new MockJwksProvider(TestJwtTokenGenerator.GetTestRsaKey(), issuer);
        var providerField = typeof(WristbandJwtValidator).GetField("_jwksProvider", BindingFlags.NonPublic | BindingFlags.Instance);
        providerField?.SetValue(validator, mockProvider);

        var validToken = TestJwtTokenGenerator.CreateValidToken(issuer);
        var result = await validator.Validate(validToken);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Payload);
        Assert.Equal(issuer, result.Payload.Iss);
        Assert.Equal("test-user-123", result.Payload.Sub);
        Assert.NotNull(result.Payload.Claims);
        Assert.True(result.Payload.Claims.Count > 0);
    }

    [Fact]
    public async Task Validate_WithNullToken_ReturnsInvalidResult()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);

        // Cast to string to resolve ambiguity
        var result = await validator.Validate((string)null!);

        Assert.False(result.IsValid);
        Assert.Contains("null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task Validate_WithEmptyToken_ReturnsInvalidResult()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);
        var result = await validator.Validate("");
        Assert.False(result.IsValid);
        Assert.Contains("null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task Validate_WithMalformedToken_ReturnsInvalidResult()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);
        var result = await validator.Validate("not-a-valid-jwt");
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Payload);
    }

    [Fact]
    public async Task Validate_WithInvalidJwtFormat_ReturnsInvalidResult()
    {
        var config = new WristbandJwtValidatorConfig { WristbandApplicationVanityDomain = "test.wristband.dev" };
        var validator = WristbandJwtValidator.Create(config);
        var result = await validator.Validate("part1.part2");
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Payload);
    }

    [Fact]
    public async Task Validate_WithExpiredToken_ReturnsExpiredError()
    {
        var domain = "test.wristband.dev";
        var issuer = $"https://{domain}";

        // Create validator normally
        var config = new WristbandJwtValidatorConfig { WristbandApplicationVanityDomain = domain };
        var validator = WristbandJwtValidator.Create(config);

        // Inject mock provider via reflection
        var mockProvider = new MockJwksProvider(TestJwtTokenGenerator.GetTestRsaKey(), issuer);
        var providerField = typeof(WristbandJwtValidator).GetField("_jwksProvider", BindingFlags.NonPublic | BindingFlags.Instance);
        providerField?.SetValue(validator, mockProvider);

        // Now validate
        var expiredToken = TestJwtTokenGenerator.CreateExpiredToken(issuer);
        var result = await validator.Validate(expiredToken);

        Assert.False(result.IsValid);
        Assert.Contains("expired", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Payload);
    }

    [Fact]
    public async Task Validate_WithInvalidSignature_ReturnsInvalidSignatureError()
    {
        var domain = "test.wristband.dev";
        var config = new WristbandJwtValidatorConfig { WristbandApplicationVanityDomain = domain };
        var validator = WristbandJwtValidator.Create(config);
        var tamperedToken = TestJwtTokenGenerator.CreateInvalidSignatureToken($"https://{domain}");
        var result = await validator.Validate(tamperedToken);
        Assert.False(result.IsValid);
        Assert.Contains("signature", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Payload);
    }

    [Fact]
    public async Task Validate_WithWrongIssuer_ReturnsInvalidIssuerError()
    {
        var domain = "test.wristband.dev";
        var issuer = $"https://{domain}";
        var config = new WristbandJwtValidatorConfig { WristbandApplicationVanityDomain = domain };
        var validator = WristbandJwtValidator.Create(config);

        var mockProvider = new MockJwksProvider(TestJwtTokenGenerator.GetTestRsaKey(), issuer);
        var providerField = typeof(WristbandJwtValidator).GetField("_jwksProvider", BindingFlags.NonPublic | BindingFlags.Instance);
        providerField?.SetValue(validator, mockProvider);

        var wrongIssuerToken = TestJwtTokenGenerator.CreateWrongIssuerToken(issuer);
        var result = await validator.Validate(wrongIssuerToken);

        Assert.False(result.IsValid);
        Assert.Contains("issuer", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Payload);
    }

    [Fact]
    public async Task Validate_HttpContext_WithValidToken_PopulatesUser()
    {
        var domain = "test.wristband.dev";
        var issuer = $"https://{domain}";
        var config = new WristbandJwtValidatorConfig { WristbandApplicationVanityDomain = domain };
        var validator = WristbandJwtValidator.Create(config);

        var mockProvider = new MockJwksProvider(TestJwtTokenGenerator.GetTestRsaKey(), issuer);
        var providerField = typeof(WristbandJwtValidator).GetField("_jwksProvider", BindingFlags.NonPublic | BindingFlags.Instance);
        providerField?.SetValue(validator, mockProvider);

        var validToken = TestJwtTokenGenerator.CreateValidToken(issuer);
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = $"Bearer {validToken}";

        var result = await validator.Validate(context);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Payload);
        Assert.NotNull(context.User);
        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Equal("test-user-123", context.User.FindFirst("sub")?.Value);
    }

    [Fact]
    public async Task Validate_HttpContext_WithNullContext_ThrowsArgumentNullException()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            validator.Validate((HttpContext)null!));
    }

    [Fact]
    public async Task Validate_HttpContext_WithMissingAuthHeader_ReturnsInvalidResult()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);
        var context = new DefaultHttpContext();

        var result = await validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.Contains("Authorization header is missing", result.ErrorMessage);
        // context.User exists but is not authenticated
        Assert.False(context.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task Validate_HttpContext_WithInvalidToken_DoesNotPopulateUser()
    {
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = "test.wristband.dev"
        };
        var validator = WristbandJwtValidator.Create(config);
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer invalid-token";

        var result = await validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        // context.User exists but is not authenticated
        Assert.False(context.User.Identity?.IsAuthenticated ?? false);
    }
}
