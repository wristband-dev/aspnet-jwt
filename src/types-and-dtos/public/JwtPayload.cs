namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Decoded JWT payload containing standard and custom claims.
/// </summary>
public class JWTPayload
{
    /// <summary>
    /// Gets or initializes the issuer (iss claim).
    /// </summary>
    public string? Iss { get; init; }

    /// <summary>
    /// Gets or initializes the subject - typically the user ID (sub claim).
    /// </summary>
    public string? Sub { get; init; }

    /// <summary>
    /// Gets or initializes the audience (aud claim) as an array.
    /// Single audience tokens will have an array with one element.
    /// </summary>
    public string[]? Aud { get; init; }

    /// <summary>
    /// Gets or initializes the expiration time as Unix timestamp in seconds (exp claim).
    /// </summary>
    public long? Exp { get; init; }

    /// <summary>
    /// Gets or initializes the issued at time as Unix timestamp in seconds (iat claim).
    /// </summary>
    public long? Iat { get; init; }

    /// <summary>
    /// Gets or initializes the not before time as Unix timestamp in seconds (nbf claim).
    /// </summary>
    public long? Nbf { get; init; }

    /// <summary>
    /// Gets or initializes the JWT ID (jti claim).
    /// </summary>
    public string? Jti { get; init; }

    /// <summary>
    /// Gets or initializes all claims as a dictionary (including standard and custom claims).
    /// </summary>
    public Dictionary<string, string>? Claims { get; init; }

    /// <summary>
    /// Gets the first audience as a string, or null if no audiences are present.
    /// Convenience method for single-audience scenarios.
    /// </summary>
    /// <returns>The first audience string, or null if the Aud array is null or empty.</returns>
    public string? GetAudienceAsString()
    {
        return Aud?.Length > 0 ? Aud[0] : null;
    }
}
