using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Wristband.AspNet.Auth.Jwt.Tests;

public class WristbandJwtBearerExtensionsTests
{
    [Fact]
    public void UseWristbandJwksValidation_ConfiguresTokenValidationParameters()
    {
        var options = new JwtBearerOptions();
        var vanityDomain = "test.wristband.dev";
        options.UseWristbandJwksValidation(
            wristbandApplicationVanityDomain: vanityDomain,
            jwksCacheMaxSize: 10,
            jwksCacheTtl: TimeSpan.FromHours(12)
        );

        Assert.NotNull(options.TokenValidationParameters);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);
        Assert.Equal($"https://{vanityDomain}", options.TokenValidationParameters.ValidIssuer);
        Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.False(options.TokenValidationParameters.ValidateAudience);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);
        Assert.NotNull(options.TokenValidationParameters.IssuerSigningKeyResolver);
    }

    [Fact]
    public void UseWristbandJwksValidation_ReturnsJwtBearerOptions()
    {
        var options = new JwtBearerOptions();
        var result = options.UseWristbandJwksValidation("test.wristband.dev");
        Assert.Same(options, result);
    }

    [Fact]
    public void UseWristbandJwksValidation_UsesDefaultCacheValues_WhenNotProvided()
    {
        var options = new JwtBearerOptions();
        options.UseWristbandJwksValidation("test.wristband.dev");

        // Can't directly verify cache size/TTL, but we can verify the method doesn't throw
        // and that TokenValidationParameters is configured
        Assert.NotNull(options.TokenValidationParameters);
    }

    [Fact]
    public void UseWristbandJwksValidation_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        JwtBearerOptions? options = null;
        var exception = Assert.Throws<ArgumentNullException>(() =>
            options!.UseWristbandJwksValidation("test.wristband.dev"));
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void UseWristbandJwksValidation_ThrowsArgumentException_WhenDomainIsNull()
    {
        var options = new JwtBearerOptions();
        var exception = Assert.Throws<ArgumentException>(() =>
            options.UseWristbandJwksValidation(null!));
        Assert.Contains("Wristband application vanity domain is required", exception.Message);
        Assert.Equal("wristbandApplicationVanityDomain", exception.ParamName);
    }

    [Fact]
    public void UseWristbandJwksValidation_ThrowsArgumentException_WhenDomainIsEmpty()
    {
        var options = new JwtBearerOptions();
        var exception = Assert.Throws<ArgumentException>(() =>
            options.UseWristbandJwksValidation(string.Empty));
        Assert.Contains("Wristband application vanity domain is required", exception.Message);
        Assert.Equal("wristbandApplicationVanityDomain", exception.ParamName);
    }

    [Fact]
    public void UseWristbandJwksValidation_AcceptsNullCacheMaxSize()
    {
        var options = new JwtBearerOptions();
        var result = options.UseWristbandJwksValidation(
            wristbandApplicationVanityDomain: "test.wristband.dev",
            jwksCacheMaxSize: null
        );
        Assert.NotNull(result.TokenValidationParameters);
    }

    [Fact]
    public void UseWristbandJwksValidation_AcceptsNullCacheTtl()
    {
        var options = new JwtBearerOptions();
        var result = options.UseWristbandJwksValidation(
            wristbandApplicationVanityDomain: "test.wristband.dev",
            jwksCacheTtl: null
        );
        Assert.NotNull(result.TokenValidationParameters);
    }

    [Fact]
    public void UseWristbandJwksValidation_ConfiguresIssuerCorrectly()
    {
        var options = new JwtBearerOptions();
        var vanityDomain = "myapp.us.wristband.dev";
        options.UseWristbandJwksValidation(vanityDomain);
        Assert.Equal($"https://{vanityDomain}", options.TokenValidationParameters.ValidIssuer);
    }

    [Fact]
    public void UseWristbandJwksValidation_CanBeCalledMultipleTimes()
    {
        var options = new JwtBearerOptions();

        // calling twice should not throw, last call wins
        options.UseWristbandJwksValidation("app1.wristband.dev");
        options.UseWristbandJwksValidation("app2.wristband.dev");
        Assert.Equal("https://app2.wristband.dev", options.TokenValidationParameters.ValidIssuer);
    }
}
