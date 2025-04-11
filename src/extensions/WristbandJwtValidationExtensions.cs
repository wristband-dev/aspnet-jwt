using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Provides extension methods for configuring Wristband JWT validation services.
/// </summary>
public static class WristbandJwtValidationExtensions
{
    /// <summary>
    /// Configures JWT validation for Wristband authentication.
    /// This sets up JSON Web Key Set (JWKS) retrieval and configures JWT validation parameters.
    /// </summary>
    /// <param name="services">The service collection to which JWT validation is added.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="WristbandJwtValidationOptions"/>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddWristbandJwtValidation(
        this IServiceCollection services,
        Action<WristbandJwtValidationOptions> configureOptions)
    {
        var options = new WristbandJwtValidationOptions();
        configureOptions(options);

        var provider = new JwksProvider(options);

        // Register the JWT Bearer scheme
        services.AddAuthentication(options =>
        {
            // Only set as default if no other scheme is configured
            if (string.IsNullOrEmpty(options.DefaultAuthenticateScheme))
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }
        })
        .AddJwtBearer(jwtOptions =>
        {
            jwtOptions.TokenValidationParameters = provider.GetTokenValidationParameters();
        });

        // Register a policy configurator that will run when AddAuthorization is called
        services.ConfigureOptions<WristbandJwtAuthorizationPolicyProvider>();

        return services;
    }
}
