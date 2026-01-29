using System.Security.Cryptography;

using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt.Tests;

internal class MockJwksProvider : IJwksProvider
{
    private readonly RSA _testRsa;
    private readonly string _issuerDomain;

    public MockJwksProvider(RSA testRsa, string issuerDomain)
    {
        _testRsa = testRsa;
        _issuerDomain = issuerDomain;
    }

    public TokenValidationParameters GetTokenValidationParameters()
    {
        var testSigningKey = new RsaSecurityKey(_testRsa);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuerDomain,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = new[] { "RS256" },
            IssuerSigningKey = testSigningKey,
        };
    }
}
