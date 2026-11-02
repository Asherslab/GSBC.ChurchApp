using System.Net;
using Azure.Core;
using GSBC.ChurchApp.Platform.Api.Email;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class GraphEmailTransportTests
{
    [Fact]
    public async Task ARefusalCarriesGraphsReasonSoTheQueueRecordsIt()
    {
        RefusingGraph graph = new("""{"error":{"code":"ErrorAccessDenied","message":"Access is denied."}}""");
        GraphEmailTransport transport = new(new HttpClient(graph), new FixedToken(), Options.Create(new GraphEmailOptions
        {
            TenantId = "tenant",
            ClientId = "client",
            Sender = "app@example.com",
            Certificate = "",
        }));

        HttpRequestException refusal = await Assert.ThrowsAsync<HttpRequestException>(() =>
            transport.SendAsync(new EmailMessage("ada@example.com", "Hello", "Hi"), TestContext.Current.CancellationToken));

        Assert.Contains("ErrorAccessDenied", refusal.Message);
        Assert.Equal("/v1.0/users/app%40example.com/sendMail", graph.Path);
    }

    private sealed class RefusingGraph(string error) : HttpMessageHandler
    {
        public string? Path { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent(error) });
        }
    }

    private sealed class FixedToken : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new("token", DateTimeOffset.MaxValue);

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }
}
