using System.Net;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.People.Api.SafeMinistry;

internal sealed class SmcGate
{
    public SemaphoreSlim One { get; } = new(1);
    public DateTimeOffset NextAt { get; set; } = DateTimeOffset.MinValue;
}

internal sealed class SafeMinistryClient(HttpClient http, IOptions<SafeMinistryOptions> options, SmcGate gate, TimeProvider time)
{
    private const int PageSize = 1000;
    private static readonly TimeSpan Spacing = TimeSpan.FromMilliseconds(250);
    private const int MostPages = 100;
    private const int MostTries = 4;
    private static readonly TimeSpan BusyWait = TimeSpan.FromSeconds(2);

    public async Task<IReadOnlyList<SmcRow>> EveryoneAsync(CancellationToken cancellationToken)
    {
        List<SmcRow> everyone = [];
        int pages = 1;
        for (int page = 1; page <= Math.Min(pages, MostPages); page++)
        {
            SmcPage found = SmcJson.Page((await GetAsync($"people?page={page}&limit={PageSize}", cancellationToken))!);
            everyone.AddRange(found.People);
            pages = found.Pages;
            if (found.People.Count == 0)
                break;
        }
        return everyone;
    }

    public async Task<SmcRow?> PersonAsync(string smcId, CancellationToken cancellationToken) =>
        await GetAsync($"people/{Uri.EscapeDataString(smcId)}", cancellationToken, missingIsNull: true) is { } json ? SmcJson.Person(json) : null;

    private async Task<string?> GetAsync(string path, CancellationToken cancellationToken, bool missingIsNull = false)
    {
        SafeMinistryOptions settings = options.Value;
        if (!settings.Connected)
            throw new SmcException("Safe Ministry Check isn't connected.");

        for (int attempt = 1; ; attempt++)
        {
            using HttpResponseMessage response = await SendAsync(new Uri(settings.BaseAddress, path), settings.ApiKey.Trim(), cancellationToken);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MostTries)
                continue;
            if (response.StatusCode == HttpStatusCode.NotFound && missingIsNull)
                return null;
            throw new SmcException(Words(response.StatusCode));
        }
    }

    private async Task<HttpResponseMessage> SendAsync(Uri url, string key, CancellationToken cancellationToken)
    {
        await gate.One.WaitAsync(cancellationToken);
        try
        {
            TimeSpan wait = gate.NextAt - time.GetUtcNow();
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, time, cancellationToken);

            DateTimeOffset sentAt = time.GetUtcNow();
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Add("X-API-Key", key);
            request.Headers.Accept.ParseAdd("application/json");
            HttpResponseMessage response = await ReachAsync(request, cancellationToken);
            gate.NextAt = sentAt + (response.StatusCode == HttpStatusCode.TooManyRequests ? RetryAfter(response) : Spacing);
            return response;
        }
        finally
        {
            gate.One.Release();
        }
    }

    private async Task<HttpResponseMessage> ReachAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new SmcException("The app couldn't reach Safe Ministry Check.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SmcException("Safe Ministry Check took too long to answer.");
        }
    }

    private static TimeSpan RetryAfter(HttpResponseMessage response) =>
        response.Headers.RetryAfter?.Delta is { } delta && delta > TimeSpan.Zero && delta < TimeSpan.FromMinutes(1) ? delta : BusyWait;

    private static string Words(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Safe Ministry Check didn't accept the app's API key.",
        HttpStatusCode.NotFound => "Safe Ministry Check didn't recognise the church's address.",
        HttpStatusCode.TooManyRequests => "Safe Ministry Check is busy. The app tries again at the next sync.",
        _ => $"Safe Ministry Check answered with an error ({(int)status}).",
    };
}
