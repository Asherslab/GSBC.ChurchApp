namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;

internal static class Patience
{
    public static async Task<T> TryAsync<T>(Func<CancellationToken, Task<T>> work, TimeSpan each, int attempts, TimeSpan backOff, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            using CancellationTokenSource limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limit.CancelAfter(each);
            try
            {
                return await work(limit.Token);
            }
            catch (Exception exception) when (attempt < attempts && !cancellationToken.IsCancellationRequested && IsPassing(exception))
            {
                await Task.Delay(backOff * attempt, cancellationToken);
            }
        }
    }

    private static bool IsPassing(Exception exception) =>
        exception is HttpRequestException or IOException or OperationCanceledException or System.Text.Json.JsonException;
}
