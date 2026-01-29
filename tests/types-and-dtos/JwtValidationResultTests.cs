namespace Wristband.AspNet.Auth.Jwt.Tests;

public class JwtValidationResultTests
{
    [Fact]
    public void CanCreateValidResult_WithPayload()
    {
        var payload = new JWTPayload { Sub = "user123" };
        var result = new JwtValidationResult
        {
            IsValid = true,
            Payload = payload
        };

        Assert.True(result.IsValid);
        Assert.NotNull(result.Payload);
        Assert.Equal("user123", result.Payload.Sub);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void CanCreateInvalidResult_WithErrorMessage()
    {
        var result = new JwtValidationResult
        {
            IsValid = false,
            ErrorMessage = "Token has expired"
        };

        Assert.False(result.IsValid);
        Assert.Null(result.Payload);
        Assert.Equal("Token has expired", result.ErrorMessage);
    }

    [Fact]
    public void CanCreateInvalidResult_WithNullErrorMessage()
    {
        var result = new JwtValidationResult
        {
            IsValid = false,
            ErrorMessage = null
        };

        Assert.False(result.IsValid);
        Assert.Null(result.Payload);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Properties_AreInitOnly()
    {
        var result = new JwtValidationResult { IsValid = true };
        Assert.True(result.IsValid);
    }
}
