using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Provides programmatic JWT validation for Wristband access tokens.
/// This class can be instantiated and used independently of ASP.NET Core's middleware pipeline,
/// making it suitable for use in custom authorization handlers or service layers.
/// </summary>
public class WristbandJwtValidator
{
    private readonly IJwksProvider _jwksProvider;

    // Private constructor - use factory method instead
    private WristbandJwtValidator(WristbandJwtValidatorConfig config)
    {
        // Convert public config to internal options
        var options = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = config.WristbandApplicationVanityDomain,
            JwksCacheMaxSize = config.JwksCacheMaxSize,
            JwksCacheTtl = config.JwksCacheTtl,
        };

        _jwksProvider = new JwksProvider(options);
    }

    /// <summary>
    /// Creates a new WristbandJwtValidator instance.
    /// </summary>
    /// <param name="config">Configuration for the JWT validator.</param>
    /// <returns>A configured WristbandJwtValidator instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    /// <exception cref="ArgumentException">Thrown when WristbandApplicationVanityDomain is not provided.</exception>
    public static WristbandJwtValidator Create(WristbandJwtValidatorConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrEmpty(config.WristbandApplicationVanityDomain))
        {
            throw new ArgumentException("WristbandApplicationVanityDomain is required", nameof(config));
        }

        return new WristbandJwtValidator(config);
    }

    /// <summary>
    /// Extracts the Bearer token from an Authorization header.
    /// </summary>
    /// <param name="authorizationHeader">The Authorization header value (e.g., "Bearer eyJ...").</param>
    /// <returns>The extracted JWT token.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the header is missing, malformed, or does not use the Bearer scheme.
    /// </exception>
    /// <example>
    /// <code>
    /// var authHeader = context.Request.Headers["Authorization"].ToString();
    /// var token = validator.ExtractBearerToken(authHeader);
    /// </code>
    /// </example>
    public string ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            throw new ArgumentException("Authorization header is missing");
        }

        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Authorization header must use Bearer scheme. " +
                "Expected format: 'Bearer <token>'");
        }

        var token = authorizationHeader.Substring("Bearer ".Length).Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Bearer token value is missing");
        }

        return token;
    }

    /// <summary>
    /// Validates a JWT token and returns the result.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>A JwtValidationResult indicating whether validation succeeded and containing the decoded payload or error message.</returns>
    /// <example>
    /// <code>
    /// var result = await validator.Validate(token);
    /// if (result.IsValid)
    /// {
    ///     var userId = result.Payload?.Sub;
    ///     // Use the validated token...
    /// }
    /// else
    /// {
    ///     // Handle validation error: result.ErrorMessage
    /// }
    /// </code>
    /// </example>
    public Task<JwtValidationResult> Validate(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token is null or empty",
            });
        }

        var tokenValidationParams = _jwksProvider.GetTokenValidationParameters();
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            // Validate the token
            var principal = tokenHandler.ValidateToken(token, tokenValidationParams, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            // Extract audiences as array
            var audiences = jwtToken.Audiences.ToArray();

            // Build payload from claims
            var payload = new JWTPayload
            {
                Iss = jwtToken.Issuer,
                Sub = jwtToken.Subject,
                Aud = audiences.Length > 0 ? audiences : null,
                Exp = jwtToken.ValidTo != DateTime.MinValue
                    ? new DateTimeOffset(jwtToken.ValidTo).ToUnixTimeSeconds()
                    : null,
                Iat = jwtToken.IssuedAt != DateTime.MinValue
                    ? new DateTimeOffset(jwtToken.IssuedAt).ToUnixTimeSeconds()
                    : null,
                Nbf = jwtToken.ValidFrom != DateTime.MinValue
                    ? new DateTimeOffset(jwtToken.ValidFrom).ToUnixTimeSeconds()
                    : null,
                Jti = jwtToken.Id,

                // Add all claims as a dictionary
                Claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value),
            };

            return Task.FromResult(new JwtValidationResult
            {
                IsValid = true,
                Payload = payload,
            });
        }
        catch (SecurityTokenExpiredException)
        {
            return Task.FromResult(new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired",
            });
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return Task.FromResult(new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid token signature",
            });
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            return Task.FromResult(new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Invalid issuer: {ex.Message}",
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Token validation failed: {ex.Message}",
            });
        }
    }

    /// <summary>
    /// Validates a JWT token from the Authorization header and automatically populates
    /// the HttpContext.User with claims from the validated token.
    /// This overload is useful for custom authorization handlers in multi-instance scenarios.
    /// </summary>
    /// <param name="context">The HTTP context containing the Authorization header.</param>
    /// <returns>A JwtValidationResult indicating success/failure. If successful, context.User is populated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    /// <example>
    /// <code>
    /// // In a custom authorization handler for multi-instance scenarios
    /// var app1Validator = WristbandJwtValidator.Create(config1);
    ///
    /// var result = await app1Validator.Validate(httpContext);
    /// if (result.IsValid)
    /// {
    ///     // context.User is now populated with JWT claims
    ///     var userId = httpContext.User.FindFirst("sub")?.Value;
    ///     context.Succeed(requirement);
    /// }
    /// </code>
    /// </example>
    public async Task<JwtValidationResult> Validate(HttpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        try
        {
            // Extract token from Authorization header
            var authHeader = context.Request.Headers["Authorization"].ToString();
            var token = ExtractBearerToken(authHeader);

            // Validate the token using the string overload
            var result = await Validate(token);

            // If valid, populate context.User with claims
            if (result.IsValid && result.Payload?.Claims != null)
            {
                var claims = result.Payload.Claims.Select(kvp => new Claim(kvp.Key, kvp.Value));
                var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
                context.User = new ClaimsPrincipal(identity);
            }

            return result;
        }
        catch (ArgumentException ex)
        {
            // Return validation failure for missing/malformed Authorization header
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
            };
        }
    }
}
