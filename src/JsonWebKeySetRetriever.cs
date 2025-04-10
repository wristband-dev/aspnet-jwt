using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Retrieves JSON Web Key Sets (JWKS) from a specified address. This class implements the
/// <see cref="IConfigurationRetriever{JsonWebKeySet}"/> interface to enable retrieval and
/// deserialization of JWKS documents.
/// </summary>
internal class JsonWebKeySetRetriever : IConfigurationRetriever<JsonWebKeySet>
{
    /// <summary>
    /// Retrieves a JSON Web Key Set from the specified address using the provided document retriever.
    /// </summary>
    /// <param name="address">The address to retrieve the JSON Web Key Set from.</param>
    /// <param name="retriever">The document retriever to use.</param>
    /// <param name="cancel">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the retrieved JSON Web Key Set.</returns>
    public Task<JsonWebKeySet> GetConfigurationAsync(
        string address,
        IDocumentRetriever retriever,
        CancellationToken cancel)
    {
        return retriever.GetDocumentAsync(address, cancel).ContinueWith(task => new JsonWebKeySet(task.Result), cancel);
    }
}
