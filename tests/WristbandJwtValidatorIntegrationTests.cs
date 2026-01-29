using Wristband.AspNet.Auth.Jwt;

namespace Wristband.AspNet.Auth.Jwt.Tests;

/// <summary>
/// Integration test for validating real JWT tokens from Wristband.
/// Run this manually with a real token to verify end-to-end functionality.
/// </summary>
public class WristbandJwtValidatorIntegrationTest
{
    /// <summary>
    /// Manual integration test - replace the token and domain with real values.
    /// </summary>
    [Fact(Skip = "Manual test - requires real JWT token")]
    public async Task ValidateRealToken_Success()
    {
        // SETUP: Replace these values with your actual Wristband configuration
        var domain = "YOUR_APP.wristband.dev";
        var authHeader = "YOUR_REAL_HEADER_WITH_TOKEN";

        // Create validator
        var config = new WristbandJwtValidatorConfig
        {
            WristbandApplicationVanityDomain = domain,
            JwksCacheMaxSize = 20,
            JwksCacheTtl = TimeSpan.FromHours(1)
        };
        var validator = WristbandJwtValidator.Create(config);

        // Extract token
        var token = validator.ExtractBearerToken(authHeader);
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Validate token
        var result = await validator.Validate(token);

        // Assert
        Assert.True(result.IsValid, $"Token validation failed: {result.ErrorMessage}");
        Assert.NotNull(result.Payload);
        Assert.Null(result.ErrorMessage);

        // Print payload for inspection
        Console.WriteLine("Token validated successfully!");
        Console.WriteLine($"Issuer: {result.Payload.Iss}");
        Console.WriteLine($"Subject: {result.Payload.Sub}");
        Console.WriteLine($"Audience: {result.Payload.GetAudienceAsString()}");
        Console.WriteLine($"Expires: {result.Payload.Exp}");
        Console.WriteLine($"Claims count: {result.Payload.Claims?.Count ?? 0}");

        Console.WriteLine("\nAll Claims:");
        Console.WriteLine("─────────────────────────────────────");
        foreach (var claim in result.Payload.Claims ?? new Dictionary<string, string>())
        {
            Console.WriteLine($"{claim.Key,-30} {claim.Value}");
        }
        Console.WriteLine("─────────────────────────────────────");
    }

    /// <summary>
    /// Quick manual test you can run in a console app.
    /// Just paste this into Program.cs and run it.
    /// </summary>
    [Fact(Skip = "Manual test - requires real JWT token")]
    public static async Task QuickTest()
    {
        // SETUP: Replace these values with your actual Wristband configuration
        var domain = "YOUR_APP.wristband.dev";
        var token = "YOUR_REAL_TOKEN";

        try
        {
            var config = new WristbandJwtValidatorConfig
            {
                WristbandApplicationVanityDomain = domain
            };
            var validator = WristbandJwtValidator.Create(config);

            Console.WriteLine("Validating token...");
            var result = await validator.Validate(token);

            if (result.IsValid)
            {
                Console.WriteLine("✓ Token is VALID!");
                Console.WriteLine($"  Subject: {result.Payload?.Sub}");
                Console.WriteLine($"  Issuer: {result.Payload?.Iss}");
                Console.WriteLine($"  Audience: {result.Payload?.GetAudienceAsString()}");
            }
            else
            {
                Console.WriteLine($"✗ Token is INVALID: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ ERROR: {ex.Message}");
        }
    }
}
