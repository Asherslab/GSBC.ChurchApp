using System.Net;
using GSBC.ChurchApp.Features.People.Api.SafeMinistry;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

public sealed class SafeMinistryClientTests
{
    private readonly TestClock _clock = new();

    [Fact]
    public async Task EveryPageIsReadWithTheKeyAndABusyAnswerIsAskedAgainAfterItsWait()
    {
        Queue<HttpResponseMessage> answers = new([
            Json(SmcSamples.Page(1, 2, SmcSamples.Person("1", "Sam", "Example"))),
            new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Headers = { RetryAfter = new(TimeSpan.FromSeconds(1)) } },
            Json(SmcSamples.Page(2, 2, SmcSamples.Person("2", "Alex", "Example"))),
        ]);
        ScriptedSmc smc = new(_clock, answers);

        IReadOnlyList<SmcRow> everyone = await Client(smc).EveryoneAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["1", "2"], everyone.Select(person => person.Id));
        Assert.Equal(
            ["https://example-church.safeministrycheck.com.au/api/people?page=1&limit=1000", "https://example-church.safeministrycheck.com.au/api/people?page=2&limit=1000",
             "https://example-church.safeministrycheck.com.au/api/people?page=2&limit=1000"],
            smc.Asked.Select(asked => asked.Url));
        Assert.All(smc.Asked, asked => Assert.Equal("test-key", asked.Key));
    }

    [Fact]
    public async Task AKeyItRefusesSaysSoInPlainWords()
    {
        ScriptedSmc smc = new(_clock, new Queue<HttpResponseMessage>([new HttpResponseMessage(HttpStatusCode.Unauthorized)]));

        SmcException problem = await Assert.ThrowsAsync<SmcException>(() => Client(smc).EveryoneAsync(TestContext.Current.CancellationToken));

        Assert.Equal("Safe Ministry Check didn't accept the app's API key.", problem.Message);
    }

    private SafeMinistryClient Client(ScriptedSmc smc) =>
        new(new HttpClient(smc), Options.Create(new SafeMinistryOptions { Subdomain = "example-church", ApiKey = "test-key" }), new SmcGate(), _clock);

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

    private sealed class ScriptedSmc(TestClock clock, Queue<HttpResponseMessage> answers) : HttpMessageHandler
    {
        public List<(string Url, string Key)> Asked { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Asked.Add((request.RequestUri!.ToString(), request.Headers.GetValues("X-API-Key").Single()));
            clock.Advance(TimeSpan.FromSeconds(1));
            return Task.FromResult(answers.Dequeue());
        }
    }
}
