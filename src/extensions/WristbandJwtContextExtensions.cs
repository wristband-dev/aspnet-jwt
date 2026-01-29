using System.Security.Claims;

using Microsoft.AspNetCore.Http;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Extension methods for accessing JWT data from HttpContext.
/// </summary>
public static class WristbandJwtContextExtensions
{
    /// <summary>
    /// The Bearer authentication scheme prefix used in Authorization headers.
    /// </summary>
    private const string BearerPrefix = "Bearer ";

    /// <summary>
    /// Gets the raw JWT token from the Authorization header.
    /// Mirrors TypeScript's req.auth.jwt pattern.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The JWT token string, or null if not present.</returns>
    public static string? GetJwt(this HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return null;
        }

        var headerValue = authHeader.ToString();
        if (string.IsNullOrEmpty(headerValue) || !headerValue.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (headerValue.Length <= BearerPrefix.Length)
        {
            return null;
        }

        var token = headerValue.Substring(BearerPrefix.Length).Trim();
        return string.IsNullOrEmpty(token) ? null : token;
    }

    /// <summary>
    /// Gets the validated JWT payload from the authenticated user's claims.
    /// Mirrors TypeScript's req.auth.jwt.payload pattern.
    /// This method assumes JWT authentication has already been performed and the user is authenticated.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The JWT payload object containing all claims.</returns>
    /// <example>
    /// <code>
    /// var jwt = context.GetJwt();
    /// var payload = context.GetJwtPayload();
    /// var userId = payload.Sub;
    /// var tenantId = payload.Claims?["tnt_id"];
    /// var appId = payload.Claims?["app_id"];
    /// </code>
    /// </example>
    public static JWTPayload GetJwtPayload(this HttpContext context)
    {
        var claims = context.User?.Claims?.ToList() ?? new List<Claim>();

        // Extract standard JWT claims
        var iss = claims.FirstOrDefault(c => c.Type == "iss")?.Value;
        var sub = claims.FirstOrDefault(c => c.Type == "sub")?.Value
            ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        var iat = claims.FirstOrDefault(c => c.Type == "iat")?.Value;
        var nbf = claims.FirstOrDefault(c => c.Type == "nbf")?.Value;
        var jti = claims.FirstOrDefault(c => c.Type == "jti")?.Value;

        // Extract audience (can be multiple)
        var audiences = claims
            .Where(c => c.Type == "aud")
            .Select(c => c.Value)
            .ToArray();

        // Build claims dictionary with ALL claims
        // Defensive: GroupBy handles duplicates by taking the last value for each claim type
        var claimsDict = claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.Last().Value);

        return new JWTPayload
        {
            Iss = iss,
            Sub = sub,
            Aud = audiences.Length > 0 ? audiences : null,
            Exp = long.TryParse(exp, out var expValue) ? expValue : null,
            Iat = long.TryParse(iat, out var iatValue) ? iatValue : null,
            Nbf = long.TryParse(nbf, out var nbfValue) ? nbfValue : null,
            Jti = jti,
            Claims = claimsDict,
        };
    }
}
