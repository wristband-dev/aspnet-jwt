using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Provides an abstraction for retrieving and managing JSON Web Key Sets (JWKS)
/// from an application's domain for JWT validation.
/// </summary>
internal interface IJwksProvider
{
    /// <summary>
    /// Generates token validation parameters based on the JWKS configuration.
    /// </summary>
    /// <returns>Token validation parameters for JWT authentication.</returns>
    TokenValidationParameters GetTokenValidationParameters();
}
