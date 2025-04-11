using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Wristband.AspNet.Auth.Jwt.Tests;

public class WristbandJwtValidationExtensionsTests
{
    [Fact]
    public void AddWristbandJwtValidation_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddWristbandJwtValidation(options =>
        {
            options.WristbandApplicationDomain = "test.wristband.dev";
        });

        var provider = services.BuildServiceProvider();

        // Verify that auth services were registered
        var authOptions = provider.GetService<IConfigureOptions<AuthenticationOptions>>();
        Assert.NotNull(authOptions);

        // Verify that JWT Bearer was registered
        var jwtOptions = provider.GetService<IConfigureOptions<JwtBearerOptions>>();
        Assert.NotNull(jwtOptions);

        // Verify that authorization policy provider was registered
        var authPolicyOptions = provider.GetService<IConfigureOptions<AuthorizationOptions>>();
        Assert.NotNull(authPolicyOptions);
    }

    [Fact]
    public void AddWristbandJwtValidation_ConfiguresJwtBearerOptions()
    {
        var services = new ServiceCollection();
        services.AddWristbandJwtValidation(options =>
        {
            options.WristbandApplicationDomain = "test.wristband.dev";
        });

        var provider = services.BuildServiceProvider();

        // Get the options instances that were configured
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = optionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);

        // Verify token validation parameters were set
        Assert.NotNull(jwtOptions.TokenValidationParameters);
        Assert.True(jwtOptions.TokenValidationParameters.ValidateIssuer);
        Assert.Equal("https://test.wristband.dev", jwtOptions.TokenValidationParameters.ValidIssuer);
        Assert.True(jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.False(jwtOptions.TokenValidationParameters.ValidateAudience);
        Assert.True(jwtOptions.TokenValidationParameters.ValidateLifetime);
        Assert.NotNull(jwtOptions.TokenValidationParameters.IssuerSigningKeyResolver);
    }

    [Fact]
    public void AddWristbandJwtValidation_SetsDefaultAuthenticationScheme_WhenNoSchemeConfigured()
    {
        var services = new ServiceCollection();
        services.AddWristbandJwtValidation(options =>
        {
            options.WristbandApplicationDomain = "test.wristband.dev";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<AuthenticationOptions>>();
        var authOptions = optionsMonitor.CurrentValue;

        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultAuthenticateScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultChallengeScheme);
    }

    [Fact]
    public void AddWristbandJwtValidation_PreservesExistingScheme_WhenAlreadyConfigured()
    {
        var services = new ServiceCollection();
        var existingScheme = "ExistingScheme";

        // Configure authentication with existing scheme
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = existingScheme;
            options.DefaultChallengeScheme = existingScheme;
        });

        services.AddWristbandJwtValidation(options =>
        {
            options.WristbandApplicationDomain = "test.wristband.dev";
        });

        var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<AuthenticationOptions>>();
        var authOptions = optionsMonitor.CurrentValue;

        Assert.Equal(existingScheme, authOptions.DefaultAuthenticateScheme);
        Assert.Equal(existingScheme, authOptions.DefaultChallengeScheme);
    }

    [Fact]
    public void AddWristbandJwtValidation_RegistersAuthorizationPolicy()
    {
        var services = new ServiceCollection();
        services.AddWristbandJwtValidation(options =>
        {
            options.WristbandApplicationDomain = "test.wristband.dev";
        });
        services.AddAuthorization();

        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>();

        // Verify policy has correct settings
        var policy = authOptions.Value.GetPolicy(WristbandJwtAuthorization.PolicyName);
        Assert.NotNull(policy);
        Assert.Contains(JwtBearerDefaults.AuthenticationScheme, policy.AuthenticationSchemes);
        Assert.Contains(policy.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public void AddWristbandJwtValidation_ThrowsException_WhenDomainNotProvided()
    {
        var services = new ServiceCollection();
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            services.AddWristbandJwtValidation(_ => { });
        });

        Assert.Contains("WristbandApplicationDomain", exception.Message);
    }

    [Fact]
    public void AddWristbandJwtValidation_ConfiguresCacheOptions()
    {
        var services = new ServiceCollection();
        var maxCacheSize = 50;
        var cacheTtl = TimeSpan.FromMinutes(30);

        services.AddWristbandJwtValidation(options =>
        {
            options.WristbandApplicationDomain = "test.wristband.dev";
            options.JwksCacheMaxSize = maxCacheSize;
            options.JwksCacheTtl = cacheTtl;
        });

        // We can only verify the service was registered successfully
        var provider = services.BuildServiceProvider();
        var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        Assert.NotNull(jwtOptions);
    }
}
