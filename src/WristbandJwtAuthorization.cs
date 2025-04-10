using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Provides constants and helpers for Wristband JWT authorization.
/// </summary>
public static class WristbandJwtAuthorization
{
    /// <summary>
    /// The policy name for Wristband JWT authorization.
    /// </summary>
    public const string PolicyName = "WristbandJwt";

    /// <summary>
    /// Gets the Wristband JWT authorization policy that can be used directly with RequireAuthorization().
    /// </summary>
    /// <returns>An AuthorizationPolicy configured for Wristband JWT validation.</returns>
    public static AuthorizationPolicy GetPolicy()
    {
        return new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build();
    }
}
