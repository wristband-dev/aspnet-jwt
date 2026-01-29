using System.Security.Claims;

using Microsoft.AspNetCore.Http;

namespace Wristband.AspNet.Auth.Jwt.Tests;

public class WristbandJwtContextExtensionsTests
{
    // ========================================
    // GetJwt() Tests
    // ========================================

    [Fact]
    public void GetJwt_WithValidBearerToken_ReturnsToken()
    {
        var context = CreateHttpContext();
        context.Request.Headers["Authorization"] = "Bearer abc123token";

        var result = context.GetJwt();

        Assert.Equal("abc123token", result);
    }

    [Fact]
    public void GetJwt_WithBearerTokenAndWhitespace_ReturnsTrimmedToken()
    {
        var context = CreateHttpContext();
        context.Request.Headers["Authorization"] = "Bearer   abc123token   ";

        var result = context.GetJwt();

        Assert.Equal("abc123token", result);
    }

    [Fact]
    public void GetJwt_WithCaseInsensitiveBearer_ReturnsToken()
    {
        var context = CreateHttpContext();
        context.Request.Headers["Authorization"] = "bearer abc123token";

        var result = context.GetJwt();

        Assert.Equal("abc123token", result);
    }

    [Fact]
    public void GetJwt_WithMixedCaseBearer_ReturnsToken()
    {
        var context = CreateHttpContext();
        context.Request.Headers["Authorization"] = "BeArEr abc123token";

        var result = context.GetJwt();

        Assert.Equal("abc123token", result);
    }

    [Fact]
    public void GetJwt_WithNoAuthorizationHeader_ReturnsNull()
    {
        var context = CreateHttpContext();

        var result = context.GetJwt();

        Assert.Null(result);
    }

    [Fact]
    public void GetJwt_WithEmptyAuthorizationHeader_ReturnsNull()
    {
        var context = CreateHttpContext();
        context.Request.Headers["Authorization"] = "";

        var result = context.GetJwt();

        Assert.Null(result);
    }

    [Fact]
    public void GetJwt_WithBearerOnly_ReturnsNull()
    {
        var context = CreateHttpContext();
        context.Request.Headers["Authorization"] = "Bearer";

        var result = context.GetJwt();

        Assert.Null(result);
    }

    [Fact]
    public void GetJwt_WithBearerAndSpaceOnly_ReturnsNull()
    {
        var context = CreateHttpContext();
        context.Request.Headers["Authorization"] = "Bearer ";

        var result = context.GetJwt();

        Assert.Null(result);
    }

    [Fact]
    public void GetJwt_WithBearerAndWhitespaceOnly_ReturnsNull()
    {
        var context = CreateHttpContext();
        context.Request.Headers["Authorization"] = "Bearer    ";

        var result = context.GetJwt();

        Assert.Null(result);
    }

    [Fact]
    public void GetJwt_WithBasicAuth_ReturnsNull()
    {
        var context = CreateHttpContext();
        context.Request.Headers["Authorization"] = "Basic dXNlcjpwYXNz";

        var result = context.GetJwt();

        Assert.Null(result);
    }

    [Fact]
    public void GetJwt_WithRandomString_ReturnsNull()
    {
        var context = CreateHttpContext();
        context.Request.Headers["Authorization"] = "RandomAuthScheme token123";

        var result = context.GetJwt();

        Assert.Null(result);
    }

    [Fact]
    public void GetJwt_WithJustToken_ReturnsNull()
    {
        var context = CreateHttpContext();
        context.Request.Headers["Authorization"] = "abc123token";

        var result = context.GetJwt();

        Assert.Null(result);
    }

    // ========================================
    // GetJwtPayload() Tests
    // ========================================

    [Fact]
    public void GetJwtPayload_WithAllStandardClaims_ReturnsPopulatedPayload()
    {
        var context = CreateHttpContext();
        var claims = new List<Claim>
        {
            new Claim("iss", "https://app.wristband.dev"),
            new Claim("sub", "user_123"),
            new Claim("aud", "api_audience"),
            new Claim("exp", "1735689600"),
            new Claim("iat", "1735603200"),
            new Claim("nbf", "1735603200"),
            new Claim("jti", "jwt_id_456")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var payload = context.GetJwtPayload();

        Assert.Equal("https://app.wristband.dev", payload.Iss);
        Assert.Equal("user_123", payload.Sub);
        Assert.NotNull(payload.Aud);
        Assert.Single(payload.Aud);
        Assert.Equal("api_audience", payload.Aud[0]);
        Assert.Equal(1735689600, payload.Exp);
        Assert.Equal(1735603200, payload.Iat);
        Assert.Equal(1735603200, payload.Nbf);
        Assert.Equal("jwt_id_456", payload.Jti);
    }

    [Fact]
    public void GetJwtPayload_WithMultipleAudiences_ReturnsAllAudiences()
    {
        var context = CreateHttpContext();
        var claims = new List<Claim>
        {
            new Claim("sub", "user_123"),
            new Claim("aud", "audience1"),
            new Claim("aud", "audience2"),
            new Claim("aud", "audience3")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var payload = context.GetJwtPayload();

        Assert.NotNull(payload.Aud);
        Assert.Equal(3, payload.Aud.Length);
        Assert.Equal("audience1", payload.Aud[0]);
        Assert.Equal("audience2", payload.Aud[1]);
        Assert.Equal("audience3", payload.Aud[2]);
    }

    [Fact]
    public void GetJwtPayload_WithNoAudience_ReturnsNullAud()
    {
        var context = CreateHttpContext();
        var claims = new List<Claim>
        {
            new Claim("sub", "user_123")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var payload = context.GetJwtPayload();

        Assert.Null(payload.Aud);
    }

    [Fact]
    public void GetJwtPayload_WithCustomClaims_IncludesInClaimsDictionary()
    {
        var context = CreateHttpContext();
        var claims = new List<Claim>
        {
            new Claim("sub", "user_123"),
            new Claim("tnt_id", "tenant_456"),
            new Claim("app_id", "app_789"),
            new Claim("van_dom", "myapp.wristband.dev"),
            new Claim("sub_kind", "user"),
            new Claim("client_id", "client_abc"),
            new Claim("custom_claim", "custom_value")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var payload = context.GetJwtPayload();

        Assert.NotNull(payload.Claims);
        Assert.Equal("tenant_456", payload.Claims["tnt_id"]);
        Assert.Equal("app_789", payload.Claims["app_id"]);
        Assert.Equal("myapp.wristband.dev", payload.Claims["van_dom"]);
        Assert.Equal("user", payload.Claims["sub_kind"]);
        Assert.Equal("client_abc", payload.Claims["client_id"]);
        Assert.Equal("custom_value", payload.Claims["custom_claim"]);
    }

    [Fact]
    public void GetJwtPayload_WithInvalidExpTimestamp_ReturnsNullExp()
    {
        var context = CreateHttpContext();
        var claims = new List<Claim>
        {
            new Claim("sub", "user_123"),
            new Claim("exp", "not_a_number")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var payload = context.GetJwtPayload();

        Assert.Null(payload.Exp);
    }

    [Fact]
    public void GetJwtPayload_WithInvalidIatTimestamp_ReturnsNullIat()
    {
        var context = CreateHttpContext();
        var claims = new List<Claim>
        {
            new Claim("sub", "user_123"),
            new Claim("iat", "invalid")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var payload = context.GetJwtPayload();

        Assert.Null(payload.Iat);
    }

    [Fact]
    public void GetJwtPayload_WithInvalidNbfTimestamp_ReturnsNullNbf()
    {
        var context = CreateHttpContext();
        var claims = new List<Claim>
        {
            new Claim("sub", "user_123"),
            new Claim("nbf", "bad_timestamp")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var payload = context.GetJwtPayload();

        Assert.Null(payload.Nbf);
    }

    [Fact]
    public void GetJwtPayload_WithNoClaims_ReturnsEmptyPayload()
    {
        var context = CreateHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "TestAuth"));

        var payload = context.GetJwtPayload();

        Assert.Null(payload.Iss);
        Assert.Null(payload.Sub);
        Assert.Null(payload.Aud);
        Assert.Null(payload.Exp);
        Assert.Null(payload.Iat);
        Assert.Null(payload.Nbf);
        Assert.Null(payload.Jti);
        Assert.NotNull(payload.Claims);
        Assert.Empty(payload.Claims);
    }

    [Fact]
    public void GetJwtPayload_WithUnauthenticatedUser_ReturnsEmptyPayload()
    {
        var context = CreateHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated

        var payload = context.GetJwtPayload();

        Assert.Null(payload.Iss);
        Assert.Null(payload.Sub);
        Assert.Null(payload.Aud);
        Assert.Null(payload.Exp);
        Assert.Null(payload.Iat);
        Assert.Null(payload.Nbf);
        Assert.Null(payload.Jti);
        Assert.NotNull(payload.Claims);
        Assert.Empty(payload.Claims);
    }

    [Fact]
    public void GetJwtPayload_WithDuplicateNonAudClaims_UsesLastValue()
    {
        var context = CreateHttpContext();
        var claims = new List<Claim>
        {
            new Claim("sub", "user_123"),
            new Claim("custom", "value1"),
            new Claim("custom", "value2")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var payload = context.GetJwtPayload();

        // Dictionary will use the last value for duplicate keys
        Assert.NotNull(payload.Claims);
        Assert.Equal("value2", payload.Claims["custom"]);
    }

    [Fact]
    public void GetJwtPayload_WithAllClaimsIncludingStandard_IncludesEverythingInClaimsDictionary()
    {
        var context = CreateHttpContext();
        var claims = new List<Claim>
        {
            new Claim("iss", "https://app.wristband.dev"),
            new Claim("sub", "user_123"),
            new Claim("aud", "api_audience"),
            new Claim("exp", "1735689600"),
            new Claim("iat", "1735603200"),
            new Claim("nbf", "1735603200"),
            new Claim("jti", "jwt_id_456"),
            new Claim("tnt_id", "tenant_456")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var payload = context.GetJwtPayload();

        Assert.NotNull(payload.Claims);
        Assert.Equal(8, payload.Claims.Count);
        Assert.Equal("https://app.wristband.dev", payload.Claims["iss"]);
        Assert.Equal("user_123", payload.Claims["sub"]);
        Assert.Equal("api_audience", payload.Claims["aud"]);
        Assert.Equal("1735689600", payload.Claims["exp"]);
        Assert.Equal("1735603200", payload.Claims["iat"]);
        Assert.Equal("1735603200", payload.Claims["nbf"]);
        Assert.Equal("jwt_id_456", payload.Claims["jti"]);
        Assert.Equal("tenant_456", payload.Claims["tnt_id"]);
    }

    // ========================================
    // Helper Methods
    // ========================================

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext();
    }
}
