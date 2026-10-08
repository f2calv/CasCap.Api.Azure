using System.Net.Http.Headers;

namespace CasCap;

/// <summary>Adds an Azure <see cref="AccessToken" /> to outgoing HTTP requests.</summary>
public sealed class TokenCredentialBearerHandler : DelegatingHandler
{
    private readonly TokenCredential _credential;
    private readonly string[] _scopes;

    /// <summary>Initializes a handler for one or more OAuth scopes.</summary>
    public TokenCredentialBearerHandler(TokenCredential credential, params string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(scopes);
        if (scopes.Length == 0 || scopes.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("At least one non-empty OAuth scope is required.", nameof(scopes));

        _credential = credential;
        _scopes = [.. scopes];
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var token = await _credential.GetTokenAsync(
                new TokenRequestContext(_scopes),
                cancellationToken).ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}