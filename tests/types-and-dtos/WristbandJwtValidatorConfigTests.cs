namespace Wristband.AspNet.Auth.Jwt.Tests;

public class WristbandJwtValidatorConfigTests
{
    [Fact]
    public void WristbandJwtValidatorConfig_DefaultConstructor_InitializesWithDefaultValues()
    {
        var options = new WristbandJwtValidatorConfig();

        Assert.Null(options.WristbandApplicationVanityDomain);
        Assert.Null(options.JwksCacheMaxSize);
        Assert.Null(options.JwksCacheTtl);
    }

    [Fact]
    public void WristbandApplicationVanityDomain_CanBeSetAndRetrieved()
    {
        var options = new WristbandJwtValidatorConfig();
        var domain = "test.wristband.dev";

        options.WristbandApplicationVanityDomain = domain;

        Assert.Equal(domain, options.WristbandApplicationVanityDomain);
    }

    [Fact]
    public void JwksCacheMaxSize_CanBeSetAndRetrieved()
    {
        var options = new WristbandJwtValidatorConfig();
        var maxSize = 10;

        options.JwksCacheMaxSize = maxSize;

        Assert.Equal(maxSize, options.JwksCacheMaxSize);
    }

    [Fact]
    public void JwksCacheTtl_CanBeSetAndRetrieved()
    {
        var options = new WristbandJwtValidatorConfig();
        var ttl = TimeSpan.FromMinutes(15);

        options.JwksCacheTtl = ttl;

        Assert.Equal(ttl, options.JwksCacheTtl);
    }

    [Fact]
    public void PropertiesCanBeSetInConstructor_UsingObjectInitializers()
    {
        var domain = "test.wristband.dev";
        var maxSize = 10;
        var ttl = TimeSpan.FromMinutes(15);

        var options = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = domain,
            JwksCacheMaxSize = maxSize,
            JwksCacheTtl = ttl
        };

        Assert.Equal(domain, options.WristbandApplicationVanityDomain);
        Assert.Equal(maxSize, options.JwksCacheMaxSize);
        Assert.Equal(ttl, options.JwksCacheTtl);
    }
}
