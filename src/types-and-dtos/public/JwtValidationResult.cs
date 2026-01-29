namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Result of JWT validation.
/// </summary>
public class JwtValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the token is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets or initializes the decoded JWT payload.
    /// Only present when IsValid is true.
    /// </summary>
    public JWTPayload? Payload { get; init; }

    /// <summary>
    /// Gets or initializes the error message explaining validation failure.
    /// Only present when IsValid is false.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
