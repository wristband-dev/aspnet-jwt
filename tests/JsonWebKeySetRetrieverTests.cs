using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

using Moq;

namespace Wristband.AspNet.Auth.Jwt.Tests;

public class JsonWebKeySetRetrieverTests
{
    [Fact]
    public async Task GetConfigurationAsync_CallsDocumentRetriever()
    {
        var testAddress = "https://test.wristband.dev/api/v1/oauth2/jwks";
        var cancellationToken = CancellationToken.None;
        var mockRetriever = new Mock<IDocumentRetriever>();

        var jsonResponse = @"{
            ""keys"": [
                {
                    ""kty"": ""RSA"",
                    ""kid"": ""testkey1"",
                    ""use"": ""sig"",
                    ""n"": ""testValueN"",
                    ""e"": ""AQAB""
                }
            ]
        }";

        mockRetriever
            .Setup(r => r.GetDocumentAsync(testAddress, cancellationToken))
            .ReturnsAsync(jsonResponse);

        var retriever = new JsonWebKeySetRetriever();
        var result = await retriever.GetConfigurationAsync(testAddress, mockRetriever.Object, cancellationToken);

        mockRetriever.Verify(r => r.GetDocumentAsync(testAddress, cancellationToken), Times.Once);
        Assert.NotNull(result);
        Assert.IsType<JsonWebKeySet>(result);
    }

    [Fact]
    public async Task GetConfigurationAsync_ReturnsValidJsonWebKeySet()
    {
        var testAddress = "https://test.wristband.dev/api/v1/oauth2/jwks";
        var cancellationToken = CancellationToken.None;
        var mockRetriever = new Mock<IDocumentRetriever>();

        var jsonResponse = @"{
            ""keys"": [
                {
                    ""kty"": ""RSA"",
                    ""kid"": ""testkey1"",
                    ""use"": ""sig"",
                    ""n"": ""testValueN"",
                    ""e"": ""AQAB""
                }
            ]
        }";

        mockRetriever
            .Setup(r => r.GetDocumentAsync(testAddress, cancellationToken))
            .ReturnsAsync(jsonResponse);

        var retriever = new JsonWebKeySetRetriever();
        var result = await retriever.GetConfigurationAsync(testAddress, mockRetriever.Object, cancellationToken);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Keys);
        Assert.Equal("testkey1", result.Keys[0].Kid);
    }

    [Fact]
    public async Task GetConfigurationAsync_PropagatesExceptions()
    {
        var testAddress = "https://test.wristband.dev/api/v1/oauth2/jwks";
        var cancellationToken = CancellationToken.None;
        var mockRetriever = new Mock<IDocumentRetriever>();
        var expectedException = new InvalidOperationException("Test exception");

        mockRetriever
            .Setup(r => r.GetDocumentAsync(testAddress, cancellationToken))
            .ThrowsAsync(expectedException);

        var retriever = new JsonWebKeySetRetriever();
        var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
            await retriever.GetConfigurationAsync(testAddress, mockRetriever.Object, cancellationToken));

        // Verify the inner exception is our expected exception
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal(expectedException.Message, exception.InnerException.Message);
    }

    [Fact]
    public async Task GetConfigurationAsync_HandlesInvalidJson()
    {
        var testAddress = "https://test.wristband.dev/api/v1/oauth2/jwks";
        var cancellationToken = CancellationToken.None;
        var mockRetriever = new Mock<IDocumentRetriever>();
        var invalidJsonResponse = "This is not valid JSON";

        mockRetriever
            .Setup(r => r.GetDocumentAsync(testAddress, cancellationToken))
            .ReturnsAsync(invalidJsonResponse);

        var retriever = new JsonWebKeySetRetriever();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await retriever.GetConfigurationAsync(testAddress, mockRetriever.Object, cancellationToken));
    }

    [Fact]
    public void GetConfigurationAsync_RespectsCancellationToken()
    {
        var testAddress = "https://test.wristband.dev/api/v1/oauth2/jwks";
        var cancellationTokenSource = new CancellationTokenSource();
        var mockRetriever = new Mock<IDocumentRetriever>();

        mockRetriever
            .Setup(r => r.GetDocumentAsync(testAddress, It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((address, token) =>
            {
                // Create a task that will be canceled
                return Task.Delay(1000, token).ContinueWith(t => "{}", token);
            });

        var retriever = new JsonWebKeySetRetriever();

        cancellationTokenSource.Cancel();
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await retriever.GetConfigurationAsync(testAddress, mockRetriever.Object, cancellationTokenSource.Token));
    }
}
