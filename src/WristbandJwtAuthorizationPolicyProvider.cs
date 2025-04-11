using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Configures the Wristband JWT authorization policy.
/// </summary>
internal class WristbandJwtAuthorizationPolicyProvider : IConfigureOptions<AuthorizationOptions>
{
    /// <summary>
    /// Configures the Wristband JWT policy in the authorization options.
    /// </summary>
    /// <param name="options">The authorization options to configure.</param>
    public void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(
            WristbandJwtAuthorization.PolicyName,
            policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());
    }
}
