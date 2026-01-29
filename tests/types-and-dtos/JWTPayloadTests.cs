namespace Wristband.AspNet.Auth.Jwt.Tests;

public class JWTPayloadTests
{
    [Fact]
    public void GetAudienceAsString_WithNullAud_ReturnsNull()
    {
        var payload = new JWTPayload { Aud = null };
        var result = payload.GetAudienceAsString();
        Assert.Null(result);
    }

    [Fact]
    public void GetAudienceAsString_WithEmptyArray_ReturnsNull()
    {
        var payload = new JWTPayload { Aud = Array.Empty<string>() };
        var result = payload.GetAudienceAsString();
        Assert.Null(result);
    }

    [Fact]
    public void GetAudienceAsString_WithSingleAudience_ReturnsFirstAudience()
    {
        var payload = new JWTPayload { Aud = new[] { "api.example.com" } };
        var result = payload.GetAudienceAsString();
        Assert.Equal("api.example.com", result);
    }

    [Fact]
    public void GetAudienceAsString_WithMultipleAudiences_ReturnsFirstAudience()
    {
        var payload = new JWTPayload { Aud = new[] { "api.example.com", "web.example.com" } };
        var result = payload.GetAudienceAsString();
        Assert.Equal("api.example.com", result);
    }

    [Fact]
    public void Properties_CanBeInitialized()
    {
        var claims = new Dictionary<string, string> { { "custom", "value" } };
        var payload = new JWTPayload
        {
            Iss = "https://issuer.com",
            Sub = "user123",
            Aud = new[] { "aud1", "aud2" },
            Exp = 1234567890,
            Iat = 1234567800,
            Nbf = 1234567800,
            Jti = "token-id",
            Claims = claims
        };

        Assert.Equal("https://issuer.com", payload.Iss);
        Assert.Equal("user123", payload.Sub);
        Assert.Equal(new[] { "aud1", "aud2" }, payload.Aud);
        Assert.Equal(1234567890, payload.Exp);
        Assert.Equal(1234567800, payload.Iat);
        Assert.Equal(1234567800, payload.Nbf);
        Assert.Equal("token-id", payload.Jti);
        Assert.Equal(claims, payload.Claims);
    }
}
