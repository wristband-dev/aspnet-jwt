using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Provides extension methods for configuring Wristband JWT authorization.
/// </summary>
public static class WristbandJwtValidationExtensions
{
    /// <summary>
    /// Adds the "WristbandJwt" authorization policy to the authorization options.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    /// <returns>The authorization options for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Do not call this method when using the aspnet-auth package, as it registers its own
    /// multi-strategy authorization policies.
    /// </para>
    /// <para>
    /// This method registers a simple authorization policy that requires an authenticated user
    /// via the JWT Bearer authentication scheme. The policy can be used with the
    /// <c>RequireWristbandJwt()</c> endpoint extension methods.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Configure JWT Bearer authentication
    /// builder.Services.AddAuthentication()
    ///     .AddJwtBearer(options => options.UseWristbandJwksValidation(
    ///         "invotastic.us.wristband.dev"
    ///     ));
    ///
    /// // Register authorization and add the WristbandJwt policy
    /// builder.Services.AddAuthorization(options =>
    /// {
    ///     options.AddWristbandJwtPolicy();
    /// });
    ///
    /// // Use on endpoints
    /// app.MapGet("/api/data", () => "Protected")
    ///     .RequireWristbandJwt();
    /// </code>
    /// </example>
    public static AuthorizationOptions AddWristbandJwtPolicy(this AuthorizationOptions options)
    {
        options.AddPolicy("WristbandJwt", policy =>
            policy.RequireAuthenticatedUser()
                  .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));

        return options;
    }

    /// <summary>
    /// Requires Wristband JWT bearer token authentication for this endpoint.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The route handler builder for chaining.</returns>
    /// <remarks>
    /// This extension method applies the "WristbandJwt" authorization policy to the endpoint.
    /// Ensure <c>AddWristbandJwtAuthorizationPolicy()</c> has been called during service registration.
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapGet("/api/protected", () => "Hello")
    ///     .RequireWristbandJwt();
    /// </code>
    /// </example>
    public static RouteHandlerBuilder RequireWristbandJwt(this RouteHandlerBuilder builder)
    {
        return builder.RequireAuthorization("WristbandJwt");
    }

    /// <summary>
    /// Requires Wristband JWT bearer token authentication for all endpoints in this route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder for chaining.</returns>
    /// <remarks>
    /// This extension method applies the "WristbandJwt" authorization policy to all endpoints in the group.
    /// Ensure <c>AddWristbandJwtAuthorizationPolicy()</c> has been called during service registration.
    /// </remarks>
    /// <example>
    /// <code>
    /// var protectedGroup = app.MapGroup("/api/protected")
    ///     .RequireWristbandJwt();
    ///
    /// protectedGroup.MapGet("/data", () => "Hello");
    /// protectedGroup.MapPost("/update", () => "Updated");
    /// </code>
    /// </example>
    public static RouteGroupBuilder RequireWristbandJwt(this RouteGroupBuilder group)
    {
        return group.RequireAuthorization("WristbandJwt");
    }
}
