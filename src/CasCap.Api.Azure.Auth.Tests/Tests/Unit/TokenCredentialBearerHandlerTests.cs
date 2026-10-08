using Azure.Core;
using System.Net;
using System.Net.Http.Headers;

namespace CasCap.Tests.Unit;

/// <summary>Tests Azure access-token propagation for HTTP clients.</summary>
public sealed class TokenCredentialBearerHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsBearerTokenForConfiguredScope()
    {
        var credential = new RecordingTokenCredential();
        var responseHandler = new ResponseHandler();
        using var handler = new TokenCredentialBearerHandler(credential, "api://runtime/.default")
        {
            InnerHandler = responseHandler,
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://runtime.example.test/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("api://runtime/.default", Assert.Single(credential.Scopes));
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "test-token"), responseHandler.Authorization);
    }

    [Fact]
    public async Task SendAsync_PreservesCallerAuthorizationHeader()
    {
        var credential = new RecordingTokenCredential();
        var responseHandler = new ResponseHandler();
        using var handler = new TokenCredentialBearerHandler(credential, "api://runtime/.default")
        {
            InnerHandler = responseHandler,
        };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://runtime.example.test/health");
        request.Headers.Authorization = new AuthenticationHeaderValue("Custom", "caller-value");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(credential.Scopes);
        Assert.Equal(new AuthenticationHeaderValue("Custom", "caller-value"), responseHandler.Authorization);
    }

    private sealed class RecordingTokenCredential : TokenCredential
    {
        public List<string> Scopes { get; } = [];

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            Scopes.AddRange(requestContext.Scopes);
            return ValueTask.FromResult(new AccessToken("test-token", DateTimeOffset.MaxValue));
        }

    }

    private sealed class ResponseHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}