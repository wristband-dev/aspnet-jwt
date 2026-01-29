using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt.Tests;

/// <summary>
/// Helper class for generating test JWT tokens with specific validation issues.
/// </summary>
internal static class TestJwtTokenGenerator
{
    private static RSA? _testRsa;
    private static RsaSecurityKey? _testSigningKey;

    static TestJwtTokenGenerator()
    {
        InitializeKeys();
    }

    private static void InitializeKeys()
    {
        _testRsa = RSA.Create(2048);
        _testSigningKey = new RsaSecurityKey(_testRsa);
    }

    /// <summary>
    /// Creates a valid JWT token for testing.
    /// </summary>
    public static string CreateValidToken(string issuer, int expiresInMinutes = 60)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "test-user-123"),
                new Claim("sub_kind", "user"),
            }),
            Issuer = issuer,
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            SigningCredentials = new SigningCredentials(_testSigningKey, SecurityAlgorithms.RsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Creates an expired JWT token (expired 1 hour ago).
    /// </summary>
    public static string CreateExpiredToken(string issuer)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "test-user-123"),
            }),
            Issuer = issuer,
            Expires = DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
            IssuedAt = DateTime.UtcNow.AddHours(-2),
            NotBefore = DateTime.UtcNow.AddHours(-2),
            SigningCredentials = new SigningCredentials(_testSigningKey, SecurityAlgorithms.RsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Creates a JWT token with a not-before time in the future.
    /// </summary>
    public static string CreateNotYetValidToken(string issuer)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "test-user-123"),
            }),
            Issuer = issuer,
            Expires = DateTime.UtcNow.AddHours(2),
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow.AddHours(1), // Not valid for another hour
            SigningCredentials = new SigningCredentials(_testSigningKey, SecurityAlgorithms.RsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Creates a JWT token with wrong issuer.
    /// </summary>
    public static string CreateWrongIssuerToken(string correctIssuer)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "test-user-123"),
            }),
            Issuer = "https://wrong-issuer.example.com", // Wrong issuer
            Expires = DateTime.UtcNow.AddHours(1),
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            SigningCredentials = new SigningCredentials(_testSigningKey, SecurityAlgorithms.RsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Creates a JWT token with an invalid signature.
    /// </summary>
    public static string CreateInvalidSignatureToken(string issuer)
    {
        // Create a valid token first
        var validToken = CreateValidToken(issuer);

        // Tamper with the signature (last part of the JWT)
        var parts = validToken.Split('.');
        if (parts.Length == 3)
        {
            // Replace last character of signature with 'X' to invalidate it
            var signature = parts[2];
            parts[2] = signature.Substring(0, signature.Length - 1) + "X";
            return string.Join(".", parts);
        }

        return validToken;
    }

    /// <summary>
    /// Creates a JWT token with multiple audiences.
    /// </summary>
    public static string CreateTokenWithMultipleAudiences(string issuer, string[] audiences)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "test-user-123"),
            }),
            Issuer = issuer,
            Audience = audiences[0], // Primary audience
            Expires = DateTime.UtcNow.AddHours(1),
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            SigningCredentials = new SigningCredentials(_testSigningKey, SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object>
            {
                { "aud", audiences } // Multiple audiences
            }
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Gets the test RSA key for use in tests.
    /// </summary>
    public static RSA GetTestRsaKey()
    {
        return _testRsa!;
    }
}
