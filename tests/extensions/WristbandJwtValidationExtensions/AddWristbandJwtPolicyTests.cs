using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Wristband.AspNet.Auth.Jwt.Tests;

public class AddWristbandJwtPolicyTests
{
    [Fact]
    public void AddWristbandJwtPolicy_RegistersPolicy()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddWristbandJwtPolicy();
        });

        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>();
        var policy = authOptions.Value.GetPolicy("WristbandJwt");
        Assert.NotNull(policy);
        Assert.Contains(JwtBearerDefaults.AuthenticationScheme, policy.AuthenticationSchemes);
        Assert.Contains(policy.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public void AddWristbandJwtPolicy_ReturnsAuthorizationOptions()
    {
        var authOptions = new AuthorizationOptions();
        var result = authOptions.AddWristbandJwtPolicy();
        Assert.Same(authOptions, result);
    }

    [Fact]
    public void AddWristbandJwtPolicy_CanBeCalledMultipleTimes()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddWristbandJwtPolicy();
            options.AddWristbandJwtPolicy();
        });

        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>();
        var policy = authOptions.Value.GetPolicy("WristbandJwt");
        Assert.NotNull(policy);
    }
}
