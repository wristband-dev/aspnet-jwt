namespace Wristband.AspNet.Auth.Jwt.Tests;

public class WristbandJwtValidationOptionsTests
{
    [Fact]
    public void WristbandJwtValidationOptions_DefaultConstructor_InitializesWithDefaultValues()
    {
        var options = new WristbandJwtValidationOptions();

        Assert.Null(options.WristbandApplicationDomain);
        Assert.Null(options.JwksCacheMaxSize);
        Assert.Null(options.JwksCacheTtl);
    }

    [Fact]
    public void WristbandApplicationDomain_CanBeSetAndRetrieved()
    {
        var options = new WristbandJwtValidationOptions();
        var domain = "test.wristband.dev";

        options.WristbandApplicationDomain = domain;

        Assert.Equal(domain, options.WristbandApplicationDomain);
    }

    [Fact]
    public void JwksCacheMaxSize_CanBeSetAndRetrieved()
    {
        var options = new WristbandJwtValidationOptions();
        var maxSize = 10;

        options.JwksCacheMaxSize = maxSize;

        Assert.Equal(maxSize, options.JwksCacheMaxSize);
    }

    [Fact]
    public void JwksCacheTtl_CanBeSetAndRetrieved()
    {
        var options = new WristbandJwtValidationOptions();
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

        var options = new WristbandJwtValidationOptions
        {
            WristbandApplicationDomain = domain,
            JwksCacheMaxSize = maxSize,
            JwksCacheTtl = ttl
        };

        Assert.Equal(domain, options.WristbandApplicationDomain);
        Assert.Equal(maxSize, options.JwksCacheMaxSize);
        Assert.Equal(ttl, options.JwksCacheTtl);
    }
}
